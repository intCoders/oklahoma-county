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
                        var textOnly =
                            $"This is a Document Filing Notification for the Oklahoma County Clerk's office. Name: {csvData.Grantor}, Document Number: {csvData.InstrumentNumber}, Document Type: {csvData.DocumentTypeDescription}, Date Recorded: {csvData.RecordingDate}; For more information, Email: property.alert@oklahomacounty.org | Phone: 405-713-1540 | Online: https://www.okcc.online/?instrument={csvData.InstrumentNumber}";
                        bodyBuilder.AppendLine($"<h2>Document Filing Notification</h2>");
                        bodyBuilder.AppendLine($"<p>This is a Document Filing Notification from the Oklahoma County Clerk's office.</p>");
                        bodyBuilder.AppendLine($"<p><strong>Grantor:</strong> {csvData.Grantor}");
                        bodyBuilder.AppendLine($"<p><strong>Grantee:</strong> {csvData.Grantee}");
                        bodyBuilder.AppendLine($"<p><strong>Document Number:</strong> {csvData.InstrumentNumber}");
                        bodyBuilder.AppendLine($"<p><strong>Document Type:</strong> {csvData.DocumentTypeDescription}");
                        bodyBuilder.AppendLine($"<p><strong>Date Recorded:</strong> {csvData.RecordingDate}");
                        bodyBuilder.AppendLine($"<p>For more information, Email: <a href=\"mailto:property.alert@oklahomacounty.org\" target=\"_blank\">property.alert@oklahomacounty.org</a> | Phone: 405-713-1540 | Online: <a href=\"https://www.okcc.online/?instrument={csvData.InstrumentNumber}\">https://www.okcc.online/?instrument={csvData.InstrumentNumber}</a></p>");
                        bodyBuilder.AppendLine($"<p>Thanks,</p>");
                        bodyBuilder.AppendLine($"<p>The Oklahoma County Clerk</p>");

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
