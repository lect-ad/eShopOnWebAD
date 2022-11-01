using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.eShopWeb.Web.ViewModels;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.Web.Features.OrderDetails;

public class GetOrderDetailsHandler : IRequestHandler<GetOrderDetails, OrderViewModel>
{
    private readonly IReadRepository<Order> _orderRepository;
    const string TopicName = "newordercreated";
    private IConfiguration _configuration;

    public GetOrderDetailsHandler(IReadRepository<Order> orderRepository, IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _configuration = configuration;
    }

    public async Task<OrderViewModel> Handle(GetOrderDetails request,
        CancellationToken cancellationToken)
    {
        var spec = new OrderWithItemsByIdSpec(request.OrderId);
        var order = await _orderRepository.GetBySpecAsync(spec, cancellationToken);

        if (order == null)
        {
            return null;
        }

        var json_string = order.ToJson();
        
        Console.WriteLine("Sending a message to the NewOrderCreated topic...");
        SendOrderMessageAsync(json_string).GetAwaiter().GetResult();
        Console.WriteLine("Message was sent successfully.");

        return new OrderViewModel
        {
            OrderDate = order.OrderDate,
            OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
            {
                PictureUrl = oi.ItemOrdered.PictureUri,
                ProductId = oi.ItemOrdered.CatalogItemId,
                ProductName = oi.ItemOrdered.ProductName,
                UnitPrice = oi.UnitPrice,
                Units = oi.Units
            }).ToList(),
            OrderNumber = order.Id,
            ShippingAddress = order.ShipToAddress,
            Total = order.Total()
        };
    }

    async Task SendOrderMessageAsync(string messageBody)
    {
        string ServiceBusConnectionString = _configuration.GetConnectionString("ServiceBusConnection");
        //Console.WriteLine($"constring: {ServiceBusConnectionString}");
        await using var client = new ServiceBusClient(ServiceBusConnectionString);

        await using ServiceBusSender sender = client.CreateSender(TopicName);

        try
        {
            var message = new ServiceBusMessage(messageBody);
            Console.WriteLine($"Sending message: {messageBody}");
            await sender.SendMessageAsync(message);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
        }
    }
}
