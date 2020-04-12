using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShortnerService.Models
{
    public class GeoInfo
    {
            [JsonProperty("country")]
            public string Country { get; set; }

            [JsonProperty("countryCode")]
            public string CountryCode { get; set; }

            [JsonProperty("region")]
            public string Region { get; set; }

            [JsonProperty("regionName")]
            public string RegionName { get; set; }

            [JsonProperty("city")]
            public string City { get; set; }

            [JsonProperty("zip")]
            public string ZipCode { get; set; }

            [JsonProperty("timezone")]
            public string Timezone { get; set; }
        }
}
