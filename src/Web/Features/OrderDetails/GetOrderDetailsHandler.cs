using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.eShopWeb.Web.ViewModels;

namespace Microsoft.eShopWeb.Web.Features.OrderDetails;

public class GetOrderDetailsHandler : IRequestHandler<GetOrderDetails, OrderViewModel>
{
    private readonly IReadRepository<Order> _orderRepository;

    public GetOrderDetailsHandler(IReadRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository;
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

        var httpContent = new StringContent(json_string, System.Text.Encoding.UTF8, "application/json");
        var httpClient = new HttpClient();

        var httpResponse = await httpClient.PostAsync("https://order-item-reserver-task4.azurewebsites.net/api/OrderItemsReserver?clientId=blobs_extension", httpContent);
        //var httpResponse = await httpClient.PostAsync("https://order-python-task4.azurewebsites.net/api/pythonHttpTrigger1" +
        //    "?code=OD125xJdRWsb4OrBAxbXpgcTpk-iqKtRDrNQkr_jThneAzFu8KsfMA==", httpContent);

        if (httpResponse.Content != null)
        {
            var responseContent = await httpResponse.Content.ReadAsStringAsync();
        }

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
}
