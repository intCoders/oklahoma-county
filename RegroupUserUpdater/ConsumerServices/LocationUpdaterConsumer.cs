using System.Text;
using Microsoft.Extensions.Options;
using RegroupUserUpdater.Data;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

public class LocationUpdaterConsumer : BackgroundService
{
    private readonly ILogger<LocationUpdaterConsumer> _logger;
    private readonly SftpSettings _sftpSettings;
    private readonly IServiceProvider _serviceProvider;

    public LocationUpdaterConsumer(ILogger<LocationUpdaterConsumer> logger, IOptions<SftpSettings> sftpOptions,
        IServiceProvider serviceProvider)
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

                await ProcessCsvFiles(addressService, regroupService);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SFTP processing error");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task ProcessCsvFiles(IAddressService addressService, IRegroupApiService regroupApiService)
    {
        var allContacts = await regroupApiService.GetAllContactsAsync();

        var groupResults = await regroupApiService.GetGroupResultsAsync();
        List<string> notificationList = [];


        foreach (var contact in allContacts.Where(c => !string.IsNullOrWhiteSpace(c.Address)))
        {
            var addressesToSave = new List<string>();
            if (string.IsNullOrWhiteSpace(contact.Address))
            {
                continue;
            }

            var addresses = contact.Address.Split(";");

            foreach (var a in addresses)
            {
                var streetAddress = a.Split("|")[0];
                var address = await addressService.GetAddressByStreetAddressAsync(streetAddress);

                if (address == null)
                {
                    addressesToSave.Add(a);
                    notificationList.Add(contact.Address ?? "");
                    continue;
                }

                var addressParts = a.Split("|");
                if (addressParts[0].ToLower().Trim() == address.LegalAddress.ToLower().Trim() &&
                    addressParts[1].ToLower().Trim() == address.Lot.ToLower().Trim() &&
                    addressParts[2].ToLower().Trim() == address.Block.ToLower().Trim() &&
                    addressParts[3].ToLower().Trim() == address.Addition.ToLower().Trim())
                {
                    addressesToSave.Add(a);
                    continue;
                }

                var newAddress = $"{address.LegalAddress}|{address.Lot}|{address.Block}|{address.Addition}|";
                addressesToSave.Add(a);
                addressesToSave.Add(newAddress);
            }
            
            await regroupApiService.UpdateContactAddresses(contact.Email, addressesToSave);
        }

        if (notificationList.Count > 0)
        {
            var subject = "Addresses not found in local DB";
            var bodyBuilder = new StringBuilder();

            bodyBuilder.AppendLine("<h2>The following addresses were not found in the system:</h2>");
            bodyBuilder.AppendLine("<table border='1' cellpadding='5'>");
            bodyBuilder.AppendLine("<tr><th>Address</th></tr>");

            foreach (var item in notificationList)
            {
                bodyBuilder.AppendLine($"<tr><td>{item}</td></tr>");
            }

            bodyBuilder.AppendLine("</table>");

            var emails = new List<string> { "dcaballero@regroup.com" };
            await regroupApiService.SendEmailAlertAsync(subject, bodyBuilder.ToString(), emails);
        }
    }
}
