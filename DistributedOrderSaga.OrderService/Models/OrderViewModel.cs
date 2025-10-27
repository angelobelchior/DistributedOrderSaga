using DistributedOrderSaga.Contracts.Models;

namespace DistributedOrderSaga.OrderService.Models;

public record OrderViewModel(
    Guid CustomerId,
    IReadOnlyList<OrderItem> Items,
    Address ShippingAddress,
    PaymentInfo Payment)
{
    public Order ToOrder()
        => Order.Create(
            customerId: CustomerId,
            items: Items,
            shippingAddress: ShippingAddress,
            payment: Payment);
}