using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Identity;
using Azure.Core;

namespace DMF_Import_SB
{
    public class BlobNotif
    {

        [FunctionName("BlobNotif")]
        public async Task Run([BlobTrigger("dmf-import-customers/{name}", Connection = "BlobCnn")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            ImportJobMsg jobMsg = new ImportJobMsg() {
                uniqueFileName = name,
                definitionGroupId = "Ahmed-Import"
            };
            await SendMessage(jobMsg, log);
        }
        private async Task SendMessage(ImportJobMsg msg, ILogger _log)
        {
            try {
                string busCnnString = Environment.GetEnvironmentVariable("busCnnString");
            ServiceBusClient serviceBusClient = new ServiceBusClient(busCnnString, new DefaultAzureCredential());
            ServiceBusSender serviceBusSender = serviceBusClient.CreateSender("import-entities");

            ServiceBusMessage serviceBusMessage = new ServiceBusMessage(JsonConvert.SerializeObject(msg));
            serviceBusMessage.ContentType = "application/json";
            
            await serviceBusSender.SendMessageAsync(serviceBusMessage);
            }
            catch (Exception ex) {
                _log.LogInformation(ex.Message);
            }
        }
    }
}
