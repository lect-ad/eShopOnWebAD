using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace OrderItemsReserver
{
    public class OrderItemReserverSB
    {
        private IConfiguration _configuration;

        //private readonly ILogger<OrderItemReserverSB> _logger;

        public OrderItemReserverSB(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("OrderItemReserverSB")]
        public async Task Run([ServiceBusTrigger("newordercreated", "OrderItemsReserver", 
            Connection = "ServiceBusConnection", AutoComplete = true)]string SbMsg, 
            ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {SbMsg}");

            dynamic parsed = Newtonsoft.Json.Linq.JObject.Parse(SbMsg);
            Console.WriteLine(parsed);

            var shortOrderInfo = new {
                OrderItems = parsed.OrderItems,
                OrderId = parsed.Id
            };
            Console.WriteLine(shortOrderInfo);

            string order_id = parsed.Id;
            log.LogInformation($"order id -- {order_id}");
            Console.WriteLine(order_id);

            string filename = Convert.ToString(DateTime.Now, new System.Globalization.CultureInfo("nl-NL")) + "_" + order_id;

            await CreateBlob(filename + ".json", Convert.ToString(shortOrderInfo), log);

            string responseMessage = "Hello. This Service Bus triggered function executed successfully."; 
        }

        private async Task CreateBlob(string name, string data, ILogger log)
        {

            string connectionString;
            CloudStorageAccount storageAccount;
            CloudBlobClient client;
            CloudBlobContainer container;
            CloudBlockBlob blob;


            connectionString = _configuration.GetConnectionStringOrSetting("AzureWebJobsStorage");
            storageAccount = CloudStorageAccount.Parse(connectionString);
            client = storageAccount.CreateCloudBlobClient();
            container = client.GetContainerReference("orders");
            await container.CreateIfNotExistsAsync();
            blob = container.GetBlockBlobReference(name);
            blob.Properties.ContentType = "application/json";
            blob.UploadFromStreamAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data)));

        }

    }
}
