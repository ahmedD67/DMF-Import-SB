using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DMF_Import_SB
{
    public class CoreProcess
    {
        private readonly ILogger<CoreProcess> _logger;

        public CoreProcess(ILogger<CoreProcess> log)
        {
            _logger = log;
        }

        [FunctionName("CoreProcess")]
        public void Run([ServiceBusTrigger("mytopic", "mysubscription", Connection = "")]string mySbMsg)
        {
            _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
        }
    }
}
