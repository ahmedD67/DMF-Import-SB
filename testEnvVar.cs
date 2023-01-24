using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DMF_Import_SB
{
    public static class testEnvVar
    {
        [FunctionName("testEnvVar")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation(Environment.GetEnvironmentVariable("BlobCnn"));
            log.LogInformation(Environment.GetEnvironmentVariable("busCnnString"));
            log.LogInformation(Environment.GetEnvironmentVariable("busCnnStrTrigger"));
            return new OkResult();
        }
    }
}
