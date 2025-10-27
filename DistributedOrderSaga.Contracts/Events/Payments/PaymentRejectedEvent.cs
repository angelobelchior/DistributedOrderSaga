using DistributedOrderSaga.Contracts.Models;

namespace DistributedOrderSaga.Contracts.Events.Payments;

/// <summary>
/// Evento publicado quando um pagamento é rejeitado
/// </summary>
public record PaymentRejectedEvent(Guid Id, Order Order, string Reason) : IEvent
{
    public static PaymentRejectedEvent Create(Order order, string reason)
        => new(Guid.NewGuid(), order, reason);  
}