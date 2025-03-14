using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegroupUserUpdater.Models
{
    public class ContactResponse
    {
        public int Count { get; set; }
        public List<ContactResult>? Results { get; set; } = new List<ContactResult>();
    }

    public class ContactResult
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        
        public string? Email { get; set; }
        
        public string? Username { get; set; }
        
        public string? Databaseid { get; set; }
        
        public string? Emails { get; set; }
        
        [JsonPropertyName("phone_numbers")]
        public string? PhoneNumbers { get; set; }
        
        [JsonPropertyName("preferred_method")]
        public string? PreferredMethod { get; set; }
        
        [JsonPropertyName("posts_language")]
        public string? PostsLanguage { get; set; }
        
        public string? Customfield { get; set; }
        
        public string? Groupname { get; set; }
        
        public string? Address { get; set; }
    }
} 