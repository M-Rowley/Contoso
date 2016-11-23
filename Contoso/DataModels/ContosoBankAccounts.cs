using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso.DataModels
{
    public class ContosoBankAccounts
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime createdAt { get; set; }

        [JsonProperty(PropertyName = "updatedAt")]
        public DateTime updatedAt { get; set; }

        [JsonProperty(PropertyName = "deleted")]
        public bool deleted { get; set; }

        [JsonProperty(PropertyName = "balance")]
        public double balance { get; set; }
    }
}