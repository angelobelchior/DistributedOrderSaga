using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Events.Orders;

/// <summary>
/// Evento publicado quando um pedido é criado
/// </summary>
public record OrderCreatedEvent(Guid Id, Order Order) : IEvent
{
    public static OrderCreatedEvent Create(Order order) 
        => new(Guid.NewGuid(), order);
}