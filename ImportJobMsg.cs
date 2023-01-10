using System;
using Newtonsoft.Json;

namespace Full_Integration
{
    public class ImportJobMsg
    {
        [JsonProperty("uniqueFileName")]
        public string uniqueFileName { get; set; }
        [JsonProperty("definitionGroupId")]
        public string definitionGroupId { get; set; }
    }
}
