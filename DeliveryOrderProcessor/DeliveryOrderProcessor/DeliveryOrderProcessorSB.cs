using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DeliveryOrderProcessor
{
    public class DeliveryOrderProcessorSB
    {
        private readonly ILogger<DeliveryOrderProcessorSB> _logger;

        public DeliveryOrderProcessorSB(ILogger<DeliveryOrderProcessorSB> log)
        {
            _logger = log;
        }

        [FunctionName("DeliveryOrderProcessorSB")]
        public async Task Run(
            [ServiceBusTrigger("newordercreated", "DeliveryOrderProcessor", 
                Connection = "ServiceBusConnection", AutoComplete = true)]string SbMsg,
            [CosmosDB(
                databaseName: "DeliveryDB",
                collectionName: "OrdersDelivery",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut)
        {
            _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {SbMsg}");

            dynamic parsed = Newtonsoft.Json.Linq.JObject.Parse(SbMsg);

            var total = 0m;
            foreach (var item in parsed.OrderItems)
            {
                decimal unit_price_dec = (decimal)item.UnitPrice.Value;
                int units_int = (int)item.Units.Value;
                total += unit_price_dec * units_int;
            }
            parsed.TotalPrice = total;
            Console.WriteLine(parsed);

            var shortDeliveryInfo = new
            {
                ShipToAddress = parsed.ShipToAddress,
                OrderItems = parsed.OrderItems,
                TotalPrice = parsed.TotalPrice,
                OrderId = parsed.Id
            };
            Console.WriteLine(shortDeliveryInfo);

            string order_id = parsed.Id;
            _logger.LogInformation($"order id -- {order_id}");
            Console.WriteLine(order_id);

            await documentsOut.AddAsync(new
            {
                id = Guid.NewGuid().ToString(), 
                DeliveryDetails = shortDeliveryInfo
            });

            string responseMessage = "Hello. This HTTP triggered function executed successfully.";
        }
    }
}
