using System.Collections.Concurrent;
using DistributedOrderSaga.Contracts.Models.Sagas;

namespace DistributedOrderSaga.Orchestration.Repositories;

public class SagaStateRepository(ILogger<SagaStateRepository> logger)
{
    private readonly ConcurrentDictionary<Guid, SagaState> _states = new();

    public SagaState? Get(Guid orderId)
        => _states.GetValueOrDefault(orderId);

    public IEnumerable<SagaState> GetAll()
        => _states.Values.OrderByDescending(s => s.StartedAt);

    public IEnumerable<SagaState> GetByStatus(SagaStatus status)
        => _states.Values
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.StartedAt);

    public bool Exists(Guid orderId)
        => _states.ContainsKey(orderId);

    public void Save(SagaState state)
    {
        if (!_states.TryAdd(state.Order.Id, state))
        {
            logger.LogWarning("SAGA state for OrderId={OrderId} already exists", state.Order.Id);
            return;
        }

        logger.LogInformation("Saved SAGA state for OrderId={OrderId}", state.Order.Id);
    }

    public void Update(SagaState state)
    {
        if (!_states.ContainsKey(state.Order.Id))
        {
            logger.LogWarning("SAGA state not found for OrderId={OrderId}", state.Order.Id);
            return;
        }

        _states[state.Order.Id] = state;
        logger.LogDebug("Updated SAGA state for OrderId={OrderId}", state.Order.Id);
    }
    
    public SagaStatistics GetStatistics()
    {
        var states = GetAll().ToList();
        return new SagaStatistics
        {
            Total = states.Count,
            Completed = states.Count(s => s.Status == SagaStatus.Completed),
            InProgress = states.Count(s => s.Status is SagaStatus.AwaitingPayment or SagaStatus.PaymentApproved or SagaStatus.Shipping),
            Cancelled = states.Count(s => s.Status is SagaStatus.CancelledByPayment or SagaStatus.CancelledByShipping),
            Compensated = states.Count(s => s.Status is SagaStatus.Compensated or SagaStatus.Compensating),
            AverageCompletionTimeSeconds = states
                .Where(s => s.Status == SagaStatus.Completed)
                .Select(s => (s.LastUpdatedAt - s.StartedAt).TotalSeconds)
                .DefaultIfEmpty(0)
                .Average()
        };
    }
}
