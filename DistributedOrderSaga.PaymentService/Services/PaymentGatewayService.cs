using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;
using DistributedOrderSaga.PaymentService.Models;

namespace DistributedOrderSaga.PaymentService.Services;

public class PaymentGatewayService
{
    public PaymentGatewayResult ProcessPayment(PaymentInfo payment)
    {
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

    public PaymentGatewayResult ProcessRefund(Order order)
    {
        var success = Random.Shared.NextDouble() <= 0.7;
        return success
            ? new PaymentGatewayResult(PaymentStatus.Approved, "Estorno realizado com sucesso.")
            : new PaymentGatewayResult(PaymentStatus.Failed, "Falha ao processar o estorno (simulação).");
    }
}