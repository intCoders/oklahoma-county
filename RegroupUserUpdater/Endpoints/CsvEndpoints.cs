using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Endpoints
{
    public static class CsvEndpoints
    {
        public static void MapCsvEndpoints(this WebApplication app)
        {
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
                List<CsvData> notificationList = new List<CsvData>();

                var groupResults = await regroupApiService.GetGroupResultsAsync();

                foreach (var csvData in csvDataList)
                {   
                    if (csvData.TaxId == null)
                    {
                        continue;
                    }

                    ContactResult? contact = await regroupApiService.GetContactAsync(csvData.TaxId);

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
                        var newGroup = new GroupResult();
                        newGroup.Name = csvData.LongLegal;
                        newGroup.Description = csvData.LongLegal;
                        newGroup.CustomAttributes = $"lot:{csvData.Lot};block:{csvData.Block};addition:{csvData.Addition};section:Not set;township:Not set;q1:Not set;q2:Not set;q3:Not set;q4:Not set";

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

                    var updatedGroups = new List<string>(contactGroups);
                    updatedGroups.Add(csvData.LongLegal);
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
        }
    }
} 