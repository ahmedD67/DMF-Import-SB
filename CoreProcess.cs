using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs.Extensions.Storage.Blobs;
using System.Net.Http.Headers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Azure.Identity;

/*
This is for the CICD pipeline demo.
*/
namespace DMF_Import_SB
{
    public class CoreProcess
    {
        [FunctionName("CoreProcess")]
        public async Task Run([ServiceBusTrigger("import-entities", "CoreProcessSub", Connection = "busCnnStrTrigger")]string mySbMsg, ILogger _logger)
        {
            _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
            var _token = GetToken(_logger);

            
            ImportJobMsg jobMsg = JsonConvert.DeserializeObject<ImportJobMsg>(mySbMsg);
            
            _logger.LogInformation(mySbMsg);

            var baseURI = Environment.GetEnvironmentVariable("dynamicsBaseURI");
            _logger.LogInformation(baseURI);
            HttpClient _client = new HttpClient();
            _client.BaseAddress = new Uri(baseURI);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            
            
            string packageUrl = GetImportURL(_client, jobMsg, _logger);

            ImportJobPayload importPkgPayload = new ImportJobPayload()
            {
                packageUrl = packageUrl,
                definitionGroupId = jobMsg.definitionGroupId,
                executionId = jobMsg.uniqueFileName,
                execute = true,
                overwrite = true,
                legalEntityId = "DAT"
            };
            try {
            await TransferBlobs(packageUrl, jobMsg.uniqueFileName, _logger);
            await ImportPackage(_client, importPkgPayload); ;
            _logger.LogInformation("Function is complete."); }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.ToString());
            }
        }

        // Get token for DMF access
        private string GetToken(ILogger _logger)
        {
            _logger.LogInformation("Getting token.");
            var clientId = Environment.GetEnvironmentVariable("d365ClientId");
            var tenantId = Environment.GetEnvironmentVariable("d365TenantId");
            var clientSecret = Environment.GetEnvironmentVariable("d365ClientSecret");
            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithClientSecret(clientSecret)
                .Build();
            string[] scopes = {Environment.GetEnvironmentVariable("dynamicsBaseURI")+"/.default"};
            return confidentialApp.AcquireTokenForClient(scopes).ExecuteAsync().Result.AccessToken;
        }

        public string GetImportURL(HttpClient _client, ImportJobMsg jobMsg, ILogger _logger)
        {
            string uri = "";
            try {
            string endpoint = "/data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.GetAzureWriteUrl";
            string reqPayload = $"{{\"uniqueFileName\":\"{jobMsg.uniqueFileName}\"}}";
            StringContent reqContent = new StringContent(reqPayload, Encoding.UTF8, "application/json");
            string response = _client.PostAsync(endpoint, reqContent).Result.Content.ReadAsStringAsync().Result;
            _logger.LogInformation(response);
            uri = JsonConvert.DeserializeObject<WritableURLResponse>(response).GetURI();
            _logger.LogInformation($"Got ImportURI: {uri}"); }
            catch (Exception ex) {_logger.LogInformation(ex.Message); }
            return uri;
        }

        public static async Task TransferBlobs(string sinkCnnString, string uniqueFileName, ILogger log)
        {
            log.LogInformation(sinkCnnString);
            // cnn string, source container, and blobName
            string sourceSAS = Environment.GetEnvironmentVariable("BlobCnn");
            BlobClient sinkBlobClient = new BlobClient(new Uri(sinkCnnString));
            // BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
            BlobContainerClient sourceContainerClient = new BlobContainerClient(sourceSAS, "dmf-import-customers");
            BlobClient sourceBlobClient = sourceContainerClient.GetBlobClient(uniqueFileName);
            
            log.LogInformation("Sending copy blob request....");
            var result = await sinkBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
            log.LogInformation("Copy blob request sent....");
            log.LogInformation("============"); 
            bool isBlobCopiedSuccessfully = false;
            do
            {
                log.LogInformation("Checking copy status....");
                var sinkBlobProperties = await sinkBlobClient.GetPropertiesAsync();
                log.LogInformation($"Current copy status = {sinkBlobProperties.Value.CopyStatus}");
                if (sinkBlobProperties.Value.CopyStatus.ToString() == "Pending")
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    isBlobCopiedSuccessfully = sinkBlobProperties.Value.CopyStatus.ToString() == "Success";
                    break;
                }
            } while (true);
        }

        public async Task ImportPackage(HttpClient _client, ImportJobPayload jobPayload)
        {
            string endpoint = "/data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.ImportFromPackage";
            StringContent reqContent = new StringContent(JsonConvert.SerializeObject(jobPayload), Encoding.UTF8, "application/json");
            await _client.PostAsync(endpoint, reqContent);
        }
    }
}
