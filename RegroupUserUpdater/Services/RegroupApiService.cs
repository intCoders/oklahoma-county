using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Services
{
    public class RegroupApiService : IRegroupApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public RegroupApiService()
        {
            _httpClient = CreateHttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private static HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes("-jrdX_FydxxrYoKQXdvNTw:ubPYpX4Oz66K-Hu7vcxn5g");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            return httpClient;
        }

        public async Task<List<GroupResult>> GetGroupResultsAsync()
        {
            var response = await _httpClient.GetAsync("https://app.regroup.com/api/v3/groups");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var groupResponse = JsonSerializer.Deserialize<GroupResponse>(content, _jsonOptions);

            var groupResults = groupResponse?.Results ?? [];

            foreach (var group in groupResults)
            {
                group.ParseCustomAttributes(group.CustomAttributes);
            }

            return groupResults;
        }

        public async Task<UserResponse?> GetGroupUsersAsync(string groupCodedName)
        {
            var url = $"https://app.regroup.com/api/v3/groups/{groupCodedName}/users";
            var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserResponse>(content, _jsonOptions);
        }

        public async Task<List<ContactResult>> GetAllContactsAsync()
        {
            var url = $"https://app.regroup.com/api/v3/contacts?all=true";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var contactResponse = JsonSerializer.Deserialize<ContactResponse>(content, _jsonOptions);

            var contactResults = contactResponse?.Results ?? [];

            return contactResults;
        }

        public async Task<ContactResult?> GetContactAsync(string contactId, string contactName)
        {
            var url = $"https://app.regroup.com/api/v3/contacts?databaseid={contactId}&username={contactName}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var contactResponse = JsonSerializer.Deserialize<ContactResponse>(content, _jsonOptions);

            var contactResults = contactResponse?.Results ?? [];

            return contactResults.FirstOrDefault();
        }

        public async Task CreateGroupAsync(GroupResult newGroup)
        {
            var url = "https://app.regroup.com/api/v3/groups";
            
            var requestPayload = new
            {
                groups = new[]
                {
                    new
                    {
                        name = newGroup.Name,
                        description = newGroup.Description,
                        address = newGroup.Address ?? new Address(),
                        custom_attributes = newGroup.CustomAttributes
                    }
                }
            };
            
            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task AddContactToGroupsAsync(string email, List<string> groupNames)
        {
            var url = "https://app.regroup.com/api/v3/orgs/import_users";
            
            var requestPayload = new
            {
                users = new[]
                {
                    new
                    {
                        email,
                        groupname = string.Join(";", groupNames),
                        user_type = "contact"
                    }
                }
            };
            
            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> SendEmailAlertAsync(string subject, string body, List<string> emails, string fromEmail = "regroup@hotmail.com", string fromName = "Regroup")
        {
            var url = "https://app.regroup.com/api/v3/email_alerts";
            
            var requestPayload = new
            {
                email_alert = new EmailAlert
                {
                    Subject = subject,
                    Body = body,
                    Emails = emails,
                    EmailSettings = new EmailSettings
                    {
                        From = fromEmail,
                        FromName = fromName
                    }
                }
            };
            
            var jsonContent = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            
            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email alert: {ex.Message}");
                return false;
            }
        }
    }
} 