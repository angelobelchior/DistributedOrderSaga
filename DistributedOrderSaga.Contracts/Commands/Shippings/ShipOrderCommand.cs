using DistributedOrderSaga.Contracts.Models;

namespace DistributedOrderSaga.Contracts.Commands.Shippings;

/// <summary>
/// Comando para enviar um pedido
/// </summary>
public record ShipOrderCommand(Guid Id, Order Order) : ICommand
{
    public static ShipOrderCommand Create(Order order)
        => new(Guid.NewGuid(), order);   
}
