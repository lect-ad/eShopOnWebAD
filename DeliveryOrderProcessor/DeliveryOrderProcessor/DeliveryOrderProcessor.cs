using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DeliveryOrderProcessor
{
    public static class DeliveryOrderProcessor
    {
        [FunctionName("DeliveryOrderProcessor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "DeliveryDB",
                collectionName: "OrdersDelivery",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            string data = Convert.ToString(JsonConvert.DeserializeObject(requestBody));
            //Console.WriteLine(data);
            dynamic parsed = Newtonsoft.Json.Linq.JObject.Parse(data);
            
            var total = 0m;
            foreach (var item in parsed.OrderItems)
            {
                decimal unit_price_dec = (decimal)item.UnitPrice.Value;
                int units_int = (int)item.Units.Value;
                total += unit_price_dec * units_int;
            }
            parsed.TotalPrice = total;
            Console.WriteLine(parsed);

            string order_id = parsed.Id;
            log.LogInformation($"order id -- {order_id}");
            Console.WriteLine(order_id);

            //string filename = Convert.ToString(DateTime.Now, new System.Globalization.CultureInfo("nl-NL")) + "_" + order_id;

            await documentsOut.AddAsync(new
            {
                id = Guid.NewGuid().ToString(), //order_id,
                Details = parsed
            });

            string responseMessage = "Hello. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
