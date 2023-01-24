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
Steps:
1. Get Access Token using GetToken(). It uses MSAL
2. Create HTTP client to act as Postman. Put token in authn header value
3. GetImportURL() method. This will use client to GetAzureWritableURL with payload constructed from ImportJobMsg "uniqueFileName" property. Parse response and return
   BlobUrl
4. 
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
            
            _logger.LogInformation("_token: " + _token);
            
            string packageUrl = GetImportURL(_client, jobMsg, _logger);

            _logger.LogInformation("Pkg URL: " + packageUrl);

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
            await ImportPackage(_client, importPkgPayload); }
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
            string uri;
            try {
            string endpoint = "/data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.GetAzureWriteUrl";
            string reqPayload = $"{{\"uniqueFileName\":\"{jobMsg.uniqueFileName}\"}}";
            StringContent reqContent = new StringContent(reqPayload, Encoding.UTF8, "application/json");
            uri = JsonConvert.DeserializeObject<WritableURLResponse>(
                _client.PostAsync(endpoint, reqContent).Result.Content.ReadAsStringAsync().Result
            ).GetURI();
            _logger.LogInformation($"Got ImportURI: {uri}"); }
            catch (Exception ex) {_logger.LogInformation(ex); }
            return uri;
        }

        public static async Task TransferBlobs(string sinkCnnString, string uniqueFileName, ILogger log)
        {
            log.LogInformation(sinkCnnString);
            // cnn string, source container, and blobName
            string sourceSAS = Environment.GetEnvironmentVariable("BlobCnn");
            Uri sinkURI = new Uri(sinkCnnString);
            BlobClient sinkBlobClient = new BlobClient(sinkURI);

            // BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
            BlobContainerClient sourceContainerClient = new BlobContainerClient(new Uri(sourceSAS));
            BlobClient sourceBlobClient = sourceContainerClient.GetBlobClient(uniqueFileName);
            
            log.LogInformation("Sending copy blob request....");
            var result = await sinkBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
            log.LogInformation("Copy blob request sent....");
            log.LogInformation("============");
            /*
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
            } while (true); */
        }

        public async Task ImportPackage(HttpClient _client, ImportJobPayload jobPayload)
        {
            string endpoint = "/data/DataManagementDefinitionGroups/Microsoft.Dynamics.DataEntities.ImportFromPackage";
            StringContent reqContent = new StringContent(JsonConvert.SerializeObject(jobPayload), Encoding.UTF8, "application/json");
            await _client.PostAsync(endpoint, reqContent);
        }
    }
}
