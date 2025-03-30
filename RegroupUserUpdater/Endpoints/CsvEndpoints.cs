using System.Text;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Endpoints
{
    public static class CsvEndpoints
    {
        public static void MapCsvEndpoints(this WebApplication app)
        {

            app.MapPost("/notifyAddress", async (HttpRequest request, IRegroupApiService regroupApiService, ICsvService csvService) =>
            {
                if (!request.HasFormContentType)
                {
                    Console.WriteLine("Invalid content type");
                    return Results.BadRequest("Invalid content type");
                }

                var form = await request.ReadFormAsync();
                var file = form.Files.GetFile("file");

                if (file == null || file.Length == 0)
                {
                    Console.WriteLine("No file uploaded");
                    return Results.BadRequest("No file uploaded");
                }

                Console.WriteLine($"File uploaded: {file.FileName}, Size: {file.Length}");

                var csvDataList = await csvService.ParseDailyAlertCsvFileAsync(file);
                var allContacts = await regroupApiService.GetAllContactsAsync();
                foreach (var csvData in csvDataList)
                {
                    ContactResult? contact = null;

                    if (!string.IsNullOrWhiteSpace(csvData.Grantor))
                    {
                        string[] grantorParts = csvData.Grantor.Split(',');

                        foreach (var grantorPart in grantorParts)
                        {
                            var contactResponse = await regroupApiService.GetContactAsync("", grantorPart);

                            if(contactResponse != null)
                            {
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
                            var contactResponse = await regroupApiService.GetContactAsync("", granteePart);

                            if (contactResponse != null)
                            {
                                contact = contactResponse;
                                break;
                            }
                        }
                    }

                    if(contact == null && !string.IsNullOrWhiteSpace(csvData.LegalDescription) && csvData.LegalDescription.Contains("Lot:")
                    && csvData.LegalDescription.Contains("Block:"))
                    {
                        var addressParts = csvData.LegalDescription.Split(new string[] { "Lot:", "Block:" }, StringSplitOptions.RemoveEmptyEntries);

                        contact = allContacts.Find(x => {

                            if (string.IsNullOrWhiteSpace(x.Address) || !x.Address.Contains("Lot:") || !x.Address.Contains("Block:"))
                                return false;

                            var currentAddressParts = x.Address.Split(new string[] { "Lot:", "Block:" }, StringSplitOptions.RemoveEmptyEntries);

                            return addressParts[0].Trim().ToLower() == currentAddressParts[0].Trim().ToLower()
                                   && addressParts[1].Trim().ToLower() == currentAddressParts[1].Trim().ToLower()
                                   && addressParts[2].Trim().ToLower() == currentAddressParts[2].Trim().ToLower();
                            });
                    }

                    if (contact == null)
                        continue;

                    //Enviar correo
                }

                return Results.Ok();
            });


            app.MapPost("/uploadcsv", async (HttpRequest request, IRegroupApiService regroupApiService, ICsvService csvService) =>
            {
                if (!request.HasFormContentType)
                {
                    Console.WriteLine("Invalid content type");
                    return Results.BadRequest("Invalid content type");
                }

                var form = await request.ReadFormAsync();
                var file = form.Files.GetFile("file");

                if (file == null || file.Length == 0)
                {
                    Console.WriteLine("No file uploaded");
                    return Results.BadRequest("No file uploaded");
                }

                Console.WriteLine($"File uploaded: {file.FileName}, Size: {file.Length}");

                var csvDataList = await csvService.ParseCsvFileAsync(file);
                List<CsvData> notificationList = [];

                var groupResults = await regroupApiService.GetGroupResultsAsync();

                foreach (var csvData in csvDataList)
                {   
                    if (csvData.TaxId == null)
                    {
                        continue;
                    }

                    ContactResult? contact = await regroupApiService.GetContactAsync(csvData.TaxId, "");

                    if(contact == null)
                    {
                        notificationList.Add(csvData);
                        continue;
                    }

                    if (contact.Groupname == null || csvData.LongLegal == null)
                    {
                        continue;
                    }

                    var contactGroups = contact.Groupname.Split(";");

                    if (contactGroups.Any(x => x == csvData.LongLegal))
                        continue;

                    GroupResult? group = groupResults.Find(x => x.Name == csvData.LongLegal);

                    if (group == null)
                    {
                        var newGroup = new GroupResult
                        {
                            Name = csvData.LongLegal,
                            Description = csvData.LongLegal,
                            CustomAttributes = $"lot:{csvData.Lot};block:{csvData.Block};addition:{csvData.Addition};section:Not set;township:Not set;q1:Not set;q2:Not set;q3:Not set;q4:Not set"
                        };

                        if (!string.IsNullOrEmpty(csvData.StreetAddress))
                        {
                            newGroup.Address = new Address { Street = csvData.StreetAddress };
                        }
                        
                        await regroupApiService.CreateGroupAsync(newGroup);
                    }

                    if (contact.Email == null)
                    {
                        continue;
                    }

                    var updatedGroups = new List<string>(contactGroups)
                    {
                        csvData.LongLegal
                    };
                    await regroupApiService.AddContactToGroupsAsync(contact.Email, updatedGroups);
                }

                if (notificationList.Count > 0)
                {
                    var subject = "Contacts Not Found in Regroup";
                    var bodyBuilder = new StringBuilder();
                    
                    bodyBuilder.AppendLine("<h2>The following contacts were not found in the system:</h2>");
                    bodyBuilder.AppendLine("<table border='1' cellpadding='5'>");
                    bodyBuilder.AppendLine("<tr><th>Tax ID</th><th>Street Address</th><th>Addition</th><th>Lot</th><th>Block</th><th>Long Legal</th></tr>");
                    
                    foreach (var item in notificationList)
                    {
                        bodyBuilder.AppendLine($"<tr><td>{item.TaxId}</td><td>{item.StreetAddress}</td><td>{item.Addition}</td><td>{item.Lot}</td><td>{item.Block}</td><td>{item.LongLegal}</td></tr>");
                    }
                    
                    bodyBuilder.AppendLine("</table>");
                    bodyBuilder.AppendLine("<p>Please add these contacts to the system.</p>");
                    
                    var emails = new List<string> { "dharris_2594@hotmail.com" };
                    await regroupApiService.SendEmailAlertAsync(subject, bodyBuilder.ToString(), emails);
                }

                return Results.Ok(new { CsvData = csvDataList, GroupResults = groupResults });
            })
            .WithName("UploadCsv")
            .WithOpenApi();

            app.MapPost("/addresscsv", async (HttpRequest request, IRegroupApiService regroupApiService, ICsvService csvService, IAddressService addressService) =>
            {
                if (!request.HasFormContentType)
                {
                    Console.WriteLine("Invalid content type");
                    return Results.BadRequest("Invalid content type");
                }

                var form = await request.ReadFormAsync();
                var file = form.Files.GetFile("file");

                if (file == null || file.Length == 0)
                {
                    Console.WriteLine("No file uploaded");
                    return Results.BadRequest("No file uploaded");
                }

                Console.WriteLine($"File uploaded: {file.FileName}, Size: {file.Length}");

                var csvDataList = await csvService.ParseCsvFileAsync(file);
                List<CsvData> notificationList = [];

                foreach (var csvData in csvDataList)
                {
                    var address = await addressService.GetAddressByLegalAddressAsync(csvData.LongLegal ?? "");

                    if (address == null)
                    {
                        var newAddress = new Address
                        {
                            FullAddress = csvData.StreetAddress ?? "",
                            Addition = csvData.Addition,
                            Block = csvData.Block,
                            Lot = csvData.Lot,
                            LegalAddress = csvData.LongLegal
                        };

                        await addressService.AddAddressAsync(newAddress);
                    }
                    else
                    {
                        address.Addition = csvData.Addition;
                        address.Block = csvData.Block;
                        address.Lot = csvData.Lot;
                        address.FullAddress = csvData.StreetAddress ?? "";
                        address.LegalAddress = csvData.LongLegal;

                        await addressService.UpdateAddressAsync(address.Id, address);
                    }
                }
                    
                return Results.Ok(new { CsvData = csvDataList });
            })
            .WithName("AddressCsv")
            .WithOpenApi();

            app.MapPost("/syncaddressgroups", async (HttpRequest request, IRegroupApiService regroupApiService, ICsvService csvService, IAddressService addressService) =>
            {
                var allContacts = await regroupApiService.GetAllContactsAsync();

                var groupResults = await regroupApiService.GetGroupResultsAsync();
                List<string> notificationList = [];

                foreach (var contact in allContacts)
                {
                    var address = await addressService.GetAddressByStreetAddressAsync(contact.Address ?? "");

                    if (address != null)
                    {
                        GroupResult? group = groupResults.Find(x => x.Lot == address.Lot && x.Block == address.Block && x.Addition == address.Addition);

                        var contactGroups = string.IsNullOrWhiteSpace(contact.Groupname) ? [] : contact.Groupname.Split(";");

                        if (contactGroups.Any(x => x == group?.Name))
                            continue;

                        if (group == null)
                        {
                            var newGroup = new GroupResult
                            {
                                Name = address.LegalAddress,
                                Description = address.LegalAddress,
                                CustomAttributes = $"lot:{address.Lot};block:{address.Block};addition:{address.Addition};section:Not set;township:Not set;q1:Not set;q2:Not set;q3:Not set;q4:Not set"
                            };

                            if (!string.IsNullOrEmpty(address.FullAddress))
                            {
                                newGroup.Address = new Address { Street = address.FullAddress };
                            }

                            await regroupApiService.CreateGroupAsync(newGroup);
                        }

                        var updatedGroups = new List<string>(contactGroups)
                        {
                            address.LegalAddress ?? ""
                        };
                        await regroupApiService.AddContactToGroupsAsync(contact.Email, updatedGroups);
                    }
                    else
                        notificationList.Add(contact.Address ?? "");
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

                    var emails = new List<string> { "dharris_2594@hotmail.com" };
                    await regroupApiService.SendEmailAlertAsync(subject, bodyBuilder.ToString(), emails);
                }

                return Results.Ok();
            })
            .WithName("SyncAddressGroups")
            .WithOpenApi();
        }
    }
} 