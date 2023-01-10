using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DMF_Import_SB
{
    public class WritableURLResponse
    {
        [JsonProperty("@odata.context")]
        public string OdataContext { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public string GetURI()
        {
            Dictionary<string, string> d365BlobUrlValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(Value);
            return d365BlobUrlValues["BlobUrl"];
        }
    }
}
