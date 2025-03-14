using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegroupUserUpdater.Models
{
    public class EmailAlert
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public List<string>? Emails { get; set; } = new List<string>();
        
        [JsonPropertyName("email_settings")]
        public EmailSettings? EmailSettings { get; set; }
    }

    public class EmailSettings
    {
        public string? From { get; set; }
        
        [JsonPropertyName("from_name")]
        public string? FromName { get; set; }
    }
} 