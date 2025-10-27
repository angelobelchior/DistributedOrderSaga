namespace DistributedOrderSaga.PaymentService.Models;

public record PaymentGatewayResult(PaymentStatus Status, string ErrorMessage = "");