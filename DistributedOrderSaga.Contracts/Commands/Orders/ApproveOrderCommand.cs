using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Commands.Orders;

/// <summary>
/// Comando para aprovar um pedido
/// </summary>
public record ApproveOrderCommand(Guid Id, Order Order) : ICommand
{
    public static ApproveOrderCommand Create(Order order)
        => new(Guid.NewGuid(), order);   
}
