using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegroupUserUpdater.Models
{
    public class GroupResponse
    {
        public int Count { get; set; }
        public List<GroupResult>? Results { get; set; } = new List<GroupResult>();
    }

    public class GroupResult
    {
        public int Id { get; set; }
        
        [JsonPropertyName("coded_name")]
        public string? CodedName { get; set; }
        
        public string? Name { get; set; }
        public string? Description { get; set; }
        
        [JsonPropertyName("parent_group")]
        public string? ParentGroup { get; set; }
        
        public string? Access { get; set; }
        
        [JsonPropertyName("alert_moderation")]
        public bool AlertModeration { get; set; }
        
        [JsonPropertyName("group_type")]
        public string? GroupType { get; set; }
        
        public int Contacts { get; set; }
        public int Members { get; set; }
        
        public Address? Address { get; set; }
        
        [JsonPropertyName("custom_attributes")]
        public string? CustomAttributes { get; set; }
        
        public string? Lot { get; set; }
        public string? Block { get; set; }
        public string? Addition { get; set; }
        public string? Section { get; set; }
        public string? Township { get; set; }
        public string? Q1 { get; set; }
        public string? Q2 { get; set; }
        public string? Q3 { get; set; }
        public string? Q4 { get; set; }

        public void ParseCustomAttributes(string? customAttributes)
        {
            if (string.IsNullOrEmpty(customAttributes))
                return;
                
            var attributes = customAttributes.Split(';');
            foreach (var attribute in attributes)
            {
                var keyValue = attribute.Split(':');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLower();
                    var value = keyValue[1].Trim();

                    switch (key)
                    {
                        case "lot":
                            Lot = value;
                            break;
                        case "block":
                            Block = value;
                            break;
                        case "addition":
                            Addition = value;
                            break;
                        case "section":
                            Section = value;
                            break;
                        case "township":
                            Township = value;
                            break;
                        case "q1":
                            Q1 = value;
                            break;
                        case "q2":
                            Q2 = value;
                            break;
                        case "q3":
                            Q3 = value;
                            break;
                        case "q4":
                            Q4 = value;
                            break;
                    }
                }
            }
        }
    }
} 