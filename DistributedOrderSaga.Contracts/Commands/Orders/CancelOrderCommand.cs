using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Commands.Orders;

/// <summary>
/// Comando para cancelar um pedido
/// </summary>
public record CancelOrderCommand(Guid Id, Order Order, string Reason) : ICommand
{
    public static CancelOrderCommand Create(Order order, string reason)
        => new(Guid.NewGuid(), order, reason);   
}
