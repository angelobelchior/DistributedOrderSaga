using DistributedOrderSaga.Contracts.Models;

namespace DistributedOrderSaga.Contracts.Events.Payments;

/// <summary>
/// Evento publicado quando um reembolso é processado
/// </summary>
public record PaymentRefundedEvent(Guid Id, Order Order) : IEvent
{
    public static PaymentRefundedEvent Create(Order order)
        => new(Guid.NewGuid(), order);  
}
