using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person
{
    public class Attribute : IEquatable<Attribute>
    {
        public Attribute()
        {
            // create an id
            Id = Guid.NewGuid();
        }
        
        [JsonPropertyName("id")] 
        [Required]
        public Guid Id { get; set; }

        [JsonPropertyName("classification")] public AttributeClassification Classification { get; set; }

        [JsonPropertyName("value")] public string Value { get; set; }

        public bool Equals(Attribute other)
        {
            if (other == null) { return false; }

            return this.Classification.Equals(other.Classification) &&
                   this.Value.Equals(other.Value);
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AttributeClassification
    {
        Unknown,
        Education,
        Award,
        Skill
    }
}
