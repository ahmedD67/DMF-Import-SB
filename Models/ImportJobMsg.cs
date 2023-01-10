using System;
using Newtonsoft.Json;

namespace DMF_Import_SB
{
    public class ImportJobMsg
    {
        [JsonProperty("uniqueFileName")]
        public string uniqueFileName { get; set; }
        [JsonProperty("definitionGroupId")]
        public string definitionGroupId { get; set; }
    }
}
