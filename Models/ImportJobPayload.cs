using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DMF_Import_SB
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class ImportJobPayload
    {
        [JsonProperty("packageUrl")]
        public string packageUrl;

        [JsonProperty("definitionGroupId")]
        public string definitionGroupId;

        [JsonProperty("executionId")]
        public string executionId;

        [JsonProperty("execute")]
        public bool execute;

        [JsonProperty("overwrite")]
        public bool overwrite;

        [JsonProperty("legalEntityId")]
        public string legalEntityId;
    }


}