using DistributedOrderSaga.Contracts.Models.Orders;
using DistributedOrderSaga.PaymentService.Models;

namespace DistributedOrderSaga.PaymentService.Services;

public class PaymentGatewayService
{
    public async Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentInfo payment,
        CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(500, 800), cancellationToken);

        if (string.IsNullOrWhiteSpace(payment.CardNumber) || payment.CardNumber.Length < 12)
            return new PaymentGatewayResult(PaymentStatus.Failed,
                "Número do cartão inválido.");

        if (payment.ExpiryDate < DateTime.UtcNow)
            return new PaymentGatewayResult(PaymentStatus.Failed,
                "Cartão expirado.");

        var random = new Random();
        var approved = random.NextDouble() > 0.5;
        return approved
            ? new PaymentGatewayResult(PaymentStatus.Approved)
            : new PaymentGatewayResult(PaymentStatus.Declined, "Transação recusada pelo banco.");
    }

    public async Task<PaymentGatewayResult> ProcessRefundAsync(Order order, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(500, 800), cancellationToken);

        var success = Random.Shared.NextDouble() <= 0.7;
        return success
            ? new PaymentGatewayResult(PaymentStatus.Approved, "Estorno realizado com sucesso.")
            : new PaymentGatewayResult(PaymentStatus.Failed, "Falha ao processar o estorno (simulação).");
    }
}