using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Commands.Payments;

/// <summary>
/// Comando para reembolsar um pagamento
/// </summary>
public record RefundPaymentCommand(Guid Id, Order Order, string Reason) : ICommand
{
    public static RefundPaymentCommand Create(Order order, string reason)
        => new(Guid.NewGuid(), order, reason);   
}
