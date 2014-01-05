using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkman.Common.Models
{
    public class Registrations
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "authenticationToken")]
        public string AuthenticationToken { get; set; }

        [JsonProperty(PropertyName = "handle")]
        public string Handle { get; set; }

        [JsonProperty(PropertyName = "latitude")]
        public double Latitude { get; set; }

        [JsonProperty(PropertyName = "longitude")]
        public double Longitude { get; set; }

        [JsonProperty(PropertyName = "reminderInterval")]
        public int ReminderInterval { get; set; }
    }
}
