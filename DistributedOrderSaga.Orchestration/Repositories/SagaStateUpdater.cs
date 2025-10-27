using DistributedOrderSaga.Orchestration.Models;

namespace DistributedOrderSaga.Orchestration.Repositories;

public class SagaStateUpdater(ILogger<SagaStateUpdater> logger)
{
    public void TransitionToStatus(SagaState state, SagaStatus newStatus, SagaEvent sagaEvent)
    {
        var oldStatus = state.Status;
        state.Status = newStatus;
        state.LastUpdatedAt = DateTime.UtcNow;

        state.History.Add(new SagaStateTransition
        {
            Timestamp = DateTime.UtcNow,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Event = sagaEvent
        });

        logger.LogInformation(
            "Transitioned SAGA state for OrderId={OrderId}: {OldStatus} → {NewStatus} (Event: {Event})",
            state.Order.Id, oldStatus, newStatus, sagaEvent.Name);
    }

    public void UpdatePaymentInfo(SagaState state, Guid paymentId, bool approved)
    {
        state.PaymentApproved = approved;
        state.LastUpdatedAt = DateTime.UtcNow;

        logger.LogInformation(
            "Updated payment info for OrderId={OrderId}: PaymentId={PaymentId}, Approved={Approved}",
            state.Order.Id, paymentId, approved);
    }

    public void SetCancellationReason(SagaState state, string reason)
    {
        state.CancellationReason = reason;
        state.LastUpdatedAt = DateTime.UtcNow;

        logger.LogInformation("Set cancellation reason for OrderId={OrderId}: {Reason}", state.Order.Id, reason);
    }

    public void AddMetadata(SagaState state, string key, object value)
    {
        state.Metadata[key] = value;
        state.LastUpdatedAt = DateTime.UtcNow;

        logger.LogDebug("Added metadata for OrderId={OrderId}: {Key}={Value}", state.Order.Id, key, value);
    }
}
