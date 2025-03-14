using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace RegroupUserUpdater.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string FullAddress { get; set; } = string.Empty;
        
        public string? Lot { get; set; }
        
        public string? Block { get; set; }
        
        public string? Addition { get; set; }
        
        public string? LegalAddress { get; set; }
        
        public string? Street { get; set; }
        public string? City { get; set; }
        
        [JsonPropertyName("address_state")]
        public string? AddressState { get; set; }
        
        public string? Zip { get; set; }
        public string? Country { get; set; }
        
        [JsonPropertyName("address_type")]
        public string? AddressType { get; set; }
        
        public string? Lat { get; set; }
        public string? Lng { get; set; }
    }
} 