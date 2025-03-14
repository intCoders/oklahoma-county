using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegroupUserUpdater.Models
{
    public class UserResponse
    {
        public int Count { get; set; }
        public List<UserResult>? Results { get; set; } = new List<UserResult>();
    }

    public class UserResult
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        
        public string? Email { get; set; }
        
        public string? Phone { get; set; }
        
        public string? Role { get; set; }
    }
} 