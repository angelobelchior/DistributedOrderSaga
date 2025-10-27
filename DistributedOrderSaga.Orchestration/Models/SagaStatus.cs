namespace DistributedOrderSaga.Orchestration.Models;

public enum SagaStatus
{
    Started,
    AwaitingPayment,
    PaymentApproved,
    Shipping,
    Completed,
    CancelledByPayment,
    CancelledByShipping,
    Compensating,
    Compensated
}
