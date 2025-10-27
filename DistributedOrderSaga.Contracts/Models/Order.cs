namespace DistributedOrderSaga.Contracts.Models;

public record Order(
    Guid Id,
    Guid CustomerId,
    IReadOnlyList<OrderItem> Items,
    Address ShippingAddress,
    PaymentInfo Payment,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public decimal Total => Items.Sum(x => x.TotalPrice);

    public static Order Create(
        Guid customerId,
        IReadOnlyList<OrderItem> items,
        Address shippingAddress,
        PaymentInfo payment)
        => new(
            Id: Guid.NewGuid(),
            CustomerId: customerId,
            Items: items,
            ShippingAddress: shippingAddress,
            Payment: payment,
            Status: OrderStatus.Created,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);

    public Order ChangeStatus(OrderStatus status)
        => this with
        {
            Status = status,
            UpdatedAt = DateTime.UtcNow
        };
}