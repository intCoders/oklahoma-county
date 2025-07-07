using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using RegroupUserUpdater.Data;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

public class NotificationsConsumer : BackgroundService
{
    private readonly ILogger<NotificationsConsumer> _logger;
    private readonly SftpSettings _sftpSettings;
    private readonly IServiceProvider _serviceProvider;

    public NotificationsConsumer(ILogger<NotificationsConsumer> logger, IOptions<SftpSettings> sftpOptions, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sftpSettings = sftpOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var addressService = scope.ServiceProvider.GetRequiredService<IAddressService>();
                var regroupService = scope.ServiceProvider.GetRequiredService<IRegroupApiService>();
                var csvService = scope.ServiceProvider.GetRequiredService<ICsvService>();

                await ProcessCsvFiles(addressService, regroupService, csvService);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SFTP processing error");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task ProcessCsvFiles(IAddressService _addressService, IRegroupApiService regroupApiService, ICsvService csvService)
    {
        using var sftp = new SftpClient(_sftpSettings.Host, _sftpSettings.Port, _sftpSettings.Username, _sftpSettings.Password);
        sftp.Connect();
        _logger.LogInformation("Connected to SFTP");

        var files = sftp.ListDirectory("/upload/dailyalerts");

        foreach (var file in files)
        {
            if (file.IsDirectory || file.IsSymbolicLink || !file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                continue;

            _logger.LogInformation($"Processing file: {file.Name}");

            using var memoryStream = new MemoryStream();
            sftp.DownloadFile(file.FullName, memoryStream);
            memoryStream.Position = 0;

            using var reader = new StreamReader(memoryStream);


            var csvDataList = await csvService.ParseDailyAlertStream(reader);
            var allContacts = await regroupApiService.GetAllContactsAsync();
            foreach (var csvData in csvDataList)
            {
                try
                {
                    ContactResult? contact = null;

                    if (!string.IsNullOrWhiteSpace(csvData.Grantor))
                    {
                        string[] grantorParts = csvData.Grantor.Split(',');

                        foreach (var grantorPart in grantorParts)
                        {
                            _logger.LogInformation("Searching for grantor part: {GrantorPart}", grantorPart);
                            var contactResponse = await regroupApiService.GetContactAsync("", grantorPart);

                            if (contactResponse != null)
                            {
                                _logger.LogInformation("🚀 Found contact: {Contact}", contactResponse.Email);
                                contact = contactResponse;
                                break;
                            }
                        }
                    }

                    var contactFoundInGrantee = false;

                    if (contact == null && !string.IsNullOrWhiteSpace(csvData.Grantee))
                    {
                        string[] granteeParts = csvData.Grantee.Split(',');

                        foreach (var granteePart in granteeParts)
                        {
                            _logger.LogInformation("Searching for grantee part: {GrantorPart}", granteePart);
                            var contactResponse = await regroupApiService.GetContactAsync("", granteePart);

                            if (contactResponse != null)
                            {
                                _logger.LogInformation("🚀 Found contact: {Contact}", contactResponse.Email);
                                contact = contactResponse;
                                contactFoundInGrantee = true;
                                break;
                            }
                        }
                    }

                    if (contact == null && !string.IsNullOrWhiteSpace(csvData.LegalDescription) &&
                        csvData.LegalDescription.Contains("Lot:")
                        && csvData.LegalDescription.Contains("Block:"))
                    {
                        var addressParts = csvData.LegalDescription.Split(new string[] { "Lot:", "Block:" },
                            StringSplitOptions.RemoveEmptyEntries);
                        
                        _logger.LogInformation("Searching for address part: {@Address}", addressParts.ToList());

                        contact = allContacts.Find(x =>
                        {

                            if (string.IsNullOrWhiteSpace(x.Address))
                                return false;

                            var addresses = x.Address.Split(new string[] { ";" },
                                StringSplitOptions.RemoveEmptyEntries);

                            foreach (var currentAddress in addresses)
                            {
                                var currentAddressParts = currentAddress.Split("|");
                                if (addressParts[0].Trim().ToLower() == currentAddressParts[3].Trim().ToLower()
                                    && addressParts[1].Trim().ToLower() == currentAddressParts[1].Trim().ToLower()
                                    && addressParts[2].Trim().ToLower() == currentAddressParts[2].Trim().ToLower())
                                    return true;
                            }

                            return false;
                        });
                    }

                    if (contact == null)
                    {
                        _logger.LogInformation("🤦 Contact not found: {@Contact}", csvData); 
                    }
                    else
                    {
                        _logger.LogInformation("🚀 Found contact: {Contact}", contact.Email);
                        
                        //Enviar correo
                        var subject = "Oklahoma County Clerk - Document Filing Notification";
                        var bodyBuilder = new StringBuilder();
                        var instrumentNumberPart = "";

                        bodyBuilder.AppendJoin(
                            $"<img alt='OKCC Alerts' src='https://prod-regroup2.s3.amazonaws.com/variants/KccEat3c4pmPoXUG5iX5yaWX/b51b17122c512a79eaefedc647cfbc53e213f21ae73616e4f4a0cbc66f00bba0?response-content-disposition=inline%3B%20filename%3D%22Image20250408083250.png%22%3B%20filename%2A%3DUTF-8%27%27Image20250408083250.png&response-content-type=image%2Fpng&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=ASIA2LG7K6RWPMGCRR6L%2F20250707%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20250707T012102Z&X-Amz-Expires=300&X-Amz-Security-Token=IQoJb3JpZ2luX2VjEFwaCXVzLWVhc3QtMSJIMEYCIQC7UlyZ5czO%2FvdZgVPFrM3aIM%2Bk0W3P0M2p2GhUKjDjigIhAJ0tGN6B05Lxopy2MHOKqeQhwKeHW6GxtaQCHO05uMmTKrwFCGUQABoMNzExMjg1NDcwMzE2Igy7HPUSVHVoOVHrPFQqmQX4cTBnfuQoi%2BoQmIBiKlY3RG1BeUqu3XL9P59AlPwbVZSdJuRIkvDQxKWlWD4ekvm5gDi0ObEHbPktUdW1s7N2KOk9QA4Oiyl8QD4o9xSUb7gXlGhNuknlXJ2AStJucdhtCwd74UsgNvPoZ5v1HiQZpPBAqUAf61N%2BvUnowaFBSadd8H7TOua81zpL4Z7rVup9ws45PXcnpd3PCowaOxZKXxI%2F%2FRgO9U%2BLQ%2B6BuJ543ZDq5JslAEpEl2CVCnkpblFBsZvFthZQO%2FX5te90NQdjyCah0gw9QT0MkCzdYMl1y7gRRVrsdl3zT%2BMZG7fOy3AS4SxN73pGwQ%2BrlKo2JGMqBzuoA6I8wtvxHaJfRSXs4fU14X9N0vxehGlubPZRzqtK%2BA5T40vjFz8U15UI6PQJdE5AzF9f6siY7xskqqnVUp7IerkXbr8Yj7fa2ArL%2BbB5Q%2FV5RcNOeQy90X1%2Bbl0GlG6tyiqkTy8SP%2BP8UMbcFbwgH2av3Sl3aowdSS5oIzDJJs%2B%2FheFZD%2FIfktndPTokiwtT%2FopDtggLI86c2ES06sAOPmR9HE%2FU10ZP81DGAXzES0%2Fafo%2F2Sysmhgfen%2BhE0sFNdIYz0XWYJ3Gg91USG785rvl1EDewGapWl3BYukQFb0nwNqAnVNJqLwhJiFodNQO9%2BqIP8vW11GgtB3H24KDfk0kLQ9jOgG%2FVOul8XsHN4MYSzB%2BKoPTkJX6g4lsLMFH1g6lThAdG1bVYZaxMQSG%2BLT%2F3rGcclfRAGL5sYLGQij6tspRfaMY53MX5yy%2F%2BXFiRQGcUpd2N0%2Br17uQ1KfjmM5otc83POnamQ0XnqkZYF9UalT9UrKhLT19qwnYs0ZHghK1stEa2NFQJwSAaDM%2BjcbKT4tqawzC%2BnKvDBjqwAdj5QaMVzdwDmgUgXCjyS%2FMQQcYsRXIz1rXUDvncakDBsaJoMSP2u78Hxz1h91f0tdsbfFnaK66IiAk0sknxA1%2BMle0T76XtY4xpR%2FcIPT6QTs%2FeW0e%2BmjXnhE%2BZ%2BXlri%2FdSXfW08VFs7mUFtCO0wmUqZZzNCgKD84kaMwv6it9lAW0UQxmVEd8iyrPHnRkbAMnJxATtoScrF98ivpvPfinIw0rBzbloICn06CvqNU59&X-Amz-SignedHeaders=host&X-Amz-Signature=9e555bd44791fc0f00c0f64e85301fde3c9aca2019316e0388a3e27c27e1ee04'");
                        bodyBuilder.AppendLine($"<h2>Document Filing Notification</h2>");
                        bodyBuilder.AppendLine($"<p>This is a Document Filing Notification from the Oklahoma County Clerk's office.</p>");
                        bodyBuilder.AppendLine($"<p><strong>Grantor:</strong> {contact.FirstName?.ToUpper()} {contact.LastName?.ToUpper()}");
                        bodyBuilder.AppendLine($"<p><strong>Grantee:</strong> {csvData.Grantee}");
                        if (!contactFoundInGrantee && !string.IsNullOrWhiteSpace(csvData.InstrumentNumber))
                        {
                            var documentNumberLen = csvData.InstrumentNumber.Length;
                            var midDocumentNumber = documentNumberLen / 2;
                            instrumentNumberPart = csvData.InstrumentNumber.Substring(0, midDocumentNumber) + '\u200B' + csvData.InstrumentNumber.Substring(midDocumentNumber);
                            
                            bodyBuilder.AppendLine($"<p><strong>Document Number:</strong> {instrumentNumberPart}");
                        }

                        bodyBuilder.AppendLine($"<p><strong>Document Type:</strong> {csvData.DocumentTypeDescription}");
                        bodyBuilder.AppendLine($"<p><strong>Date Recorded:</strong> {csvData.RecordingDate}");
                        bodyBuilder.AppendLine($"<p>For more information, Email: <a href=\"mailto:property.alert@oklahomacounty.org\" target=\"_blank\">property.alert@oklahomacounty.org</a> | Phone: 405-713-1540 {(!string.IsNullOrWhiteSpace(instrumentNumberPart) ? $"| Online: <a href=\"https://www.okcc.online/?instrument={csvData.InstrumentNumber}\">https://www.okcc.online/?instrument={csvData.InstrumentNumber}</a>" : "")}</p>");
                        bodyBuilder.AppendLine($"<p>Thanks,</p>");
                        bodyBuilder.AppendLine($"<p>The Oklahoma County Clerk</p>");

                        var textOnly =
                            $"This is a Document Filing Notification for the Oklahoma County Clerk's office. Name: {csvData.Grantor}, {(!string.IsNullOrWhiteSpace(instrumentNumberPart) ? $"Document Number: {csvData.InstrumentNumber}" : "" )}, Document Type: {csvData.DocumentTypeDescription}, Date Recorded: {csvData.RecordingDate}; For more information, Email: property.alert@oklahomacounty.org | Phone: 405-713-1540 {(!string.IsNullOrWhiteSpace(instrumentNumberPart) ? "| Online: https://www.okcc.online/?instrument={instrumentNumberPart}" : "")}";

                        
                        var emails = new List<string> { contact.Email };
                        var preferredMethod = !string.IsNullOrWhiteSpace(contact.PreferredMethod)
                            ? contact.PreferredMethod.Split("|")[0]
                            : null;

                        if (preferredMethod != null)
                        {
                            await regroupApiService.SendMessage(subject, bodyBuilder.ToString(), textOnly, emails, new List<string>{preferredMethod});
                        }
                        else
                        {
                            await regroupApiService.SendMessage(subject, bodyBuilder.ToString(), textOnly, emails);
                        }

                    }

                    
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to process row: {TaxId}, Error: {@Error}", csvData.Grantee, ex);
                }
            }
            
            sftp.DeleteFile("/upload/dailyalerts/" + file.Name);
        }

        sftp.Disconnect();
    }
}
