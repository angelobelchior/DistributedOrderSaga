using DistributedOrderSaga.Contracts.Models;

namespace DistributedOrderSaga.Contracts.Events.Shipping;

/// <summary>
/// Evento publicado quando o envio de um pedido falha
/// </summary>
public record OrderShippingFailedEvent(Guid Id, Order Order, string Reason) : IEvent
{
    public static OrderShippingFailedEvent Create(Order order, string reason)
        => new(Guid.NewGuid(), order, reason);   
}
