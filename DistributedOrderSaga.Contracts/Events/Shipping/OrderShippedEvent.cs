using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Events.Shipping;

/// <summary>
/// Evento publicado quando um pedido é enviado com sucesso
/// </summary>
public record OrderShippedEvent(Guid Id, Order Order) : IEvent
{
    public static OrderShippedEvent Create(Order order)
        => new(Guid.NewGuid(), order);   
}
