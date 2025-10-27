namespace DistributedOrderSaga.Contracts.Models.Sagas;

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
