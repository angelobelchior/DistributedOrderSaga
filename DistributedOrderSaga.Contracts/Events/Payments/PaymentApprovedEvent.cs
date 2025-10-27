using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Events.Payments;

/// <summary>
/// Evento publicado quando um pagamento é aprovado
/// </summary>
public record PaymentApprovedEvent(Guid Id, Order Order) : IEvent
{
    public static PaymentApprovedEvent Create(Order order)
        => new(Guid.NewGuid(), order);   
}
