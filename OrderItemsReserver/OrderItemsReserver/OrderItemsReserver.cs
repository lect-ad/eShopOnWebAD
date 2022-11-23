using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace OrderItemsReserver
{
    public static class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            string data = Convert.ToString(JsonConvert.DeserializeObject(requestBody));
            //Console.WriteLine(data);
            dynamic parsed = Newtonsoft.Json.Linq.JObject.Parse(data);
            Console.WriteLine(parsed);

            string order_id = parsed.Id;
            log.LogInformation($"order id -- {order_id}");
            Console.WriteLine(order_id);

            string filename = Convert.ToString(DateTime.Now, new System.Globalization.CultureInfo("nl-NL")) + "_" + order_id;

            await CreateBlob(filename + ".json", Convert.ToString(parsed), log);

            string responseMessage = "Hello. This HTTP triggered function executed successfully."; //string.IsNullOrEmpty(name)

            return new OkObjectResult(responseMessage);
        }

        private async static Task CreateBlob(string name, string data, ILogger log)
        {

            string connectionString;
            CloudStorageAccount storageAccount;
            CloudBlobClient client;
            CloudBlobContainer container;
            CloudBlockBlob blob;


            connectionString = "DefaultEndpointsProtocol=https;AccountName=orderitemsstorage;AccountKey=qr5CM6HoQFAZ6xDJ1pAsgvYUY7NVUDHQO/8DgNMO//83vXC5sWMD4SGR/KLTDRAIifW7b4bHcTZ9+AStM1C1Yw==;EndpointSuffix=core.windows.net";
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
