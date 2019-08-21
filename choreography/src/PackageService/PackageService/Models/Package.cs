using System;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PackageService.Models
{
    public class Package
    {
        public Package (
            string id,
            PackageSize size,
            double weight,
            string tag)
        {
            this.Id = id;
            this.Size = size;
            this.Weight = weight;
            this.Tag = tag;
        }

        [JsonProperty(PropertyName ="id")]
        public string Id { get; set; }

        [Range(1, 3, ErrorMessage ="Value for {0} must be between {1} and {2}.")]
        [JsonProperty(PropertyName = "size")]
        public PackageSize? Size { get; set; }

        [Required]
        [JsonProperty(PropertyName = "weight")]
        public double? Weight { get; set; }

        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; set; }
    }
}



