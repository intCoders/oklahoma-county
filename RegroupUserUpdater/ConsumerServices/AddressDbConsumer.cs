using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using RegroupUserUpdater.Data;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

public class AddressDbConsumer : BackgroundService
{
    private readonly ILogger<AddressDbConsumer> _logger;
    private readonly SftpSettings _sftpSettings;
    private readonly IServiceProvider _serviceProvider;

    public AddressDbConsumer(ILogger<AddressDbConsumer> logger, IOptions<SftpSettings> sftpOptions, IServiceProvider serviceProvider)
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
                
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                await ProcessCsvFiles(addressService);
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SFTP processing error");
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
        }
    }

    private async Task ProcessCsvFiles(IAddressService _addressService)
    {
        using var sftp = new SftpClient(_sftpSettings.Host, _sftpSettings.Port, _sftpSettings.Username, _sftpSettings.Password);
        sftp.Connect();
        _logger.LogInformation("Connected to SFTP");

        var files = sftp.ListDirectory("/upload/addressdb");

        foreach (var file in files)
        {
            if (file.IsDirectory || file.IsSymbolicLink || !file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                continue;

            _logger.LogInformation($"Processing file: {file.Name}");

            using var memoryStream = new MemoryStream();
            sftp.DownloadFile(file.FullName, memoryStream);
            memoryStream.Position = 0;

            using var reader = new StreamReader(memoryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                PrepareHeaderForMatch = args => args.Header.ToLowerInvariant()
            });

            var records = csv.GetRecords<AddressCsvRow>();
            foreach (var record in records)
            {
                try
                {
                    var address = await _addressService.GetAddressByLegalAddressAsync(record.longlegal ?? "");

                    if (address == null)
                    {
                        var newAddress = new Address
                        {
                            FullAddress = record.streetaddress ?? "",
                            Addition = record.addition,
                            Block = record.block,
                            Lot = record.lot,
                            LegalAddress = record.longlegal
                        };

                        await _addressService.AddAddressAsync(newAddress);
                    }
                    else
                    {
                        address.Addition = record.addition;
                        address.Block = record.block;
                        address.Lot = record.lot;
                        address.FullAddress = record.streetaddress ?? "";
                        address.LegalAddress = record.longlegal;

                        await _addressService.UpdateAddressAsync(address.Id, address);
                    }

                    _logger.LogInformation(
                        "Processed row: {TaxId}, {StreetAddress}, {Addition}, {Lot}, {Block}, {LongLegal}",
                        record.taxid, record.streetaddress, record.addition, record.lot, record.block,
                        record.longlegal);
                    
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to process row: {TaxId}, Error: {@Error}", record.taxid, ex);
                }
            }
        }

        sftp.Disconnect();
    }

    private class AddressCsvRow
    {
        public string taxid { get; set; }
        public string streetaddress { get; set; }
        public string addition { get; set; }
        public string lot { get; set; }
        public string block { get; set; }
        public string longlegal { get; set; }
    }
}
