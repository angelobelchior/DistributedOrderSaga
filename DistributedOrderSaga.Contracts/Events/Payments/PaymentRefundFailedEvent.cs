using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Events.Payments;

public record PaymentRefundFailedEvent(Guid Id, Order Order, string ErrorMessage) : IMessage
{
    public static PaymentRefundFailedEvent Create(Order order, string errorMessage) =>
        new(Guid.NewGuid(), order, errorMessage);
}