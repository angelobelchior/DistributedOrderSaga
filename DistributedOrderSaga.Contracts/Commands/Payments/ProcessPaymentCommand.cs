using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Commands.Payments;

/// <summary>
/// Comando para processar um pagamento
/// </summary>
public record ProcessPaymentCommand(Guid Id, Order Order) : ICommand
{
    public static ProcessPaymentCommand Create(Order order)
        => new(Guid.NewGuid(), order);
}