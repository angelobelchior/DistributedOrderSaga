namespace DistributedOrderSaga.PaymentService.Models;

public record Payment(Guid Id, Guid OrderId, PaymentStatus Status)
{
    public static Payment Create(Guid orderId, PaymentStatus status)
        => new(Guid.NewGuid(), orderId, status);

    public Payment ChangeStatus(PaymentStatus newStatus)
        => this with { Status = newStatus };

    public bool IsAlreadyProcessed(PaymentStatus status)
        => status == Status;
}

public enum PaymentStatus
{
    Approved,
    Declined,
    Failed,
    Refunded
}