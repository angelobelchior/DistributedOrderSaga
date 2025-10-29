using System.Collections.Concurrent;
using DistributedOrderSaga.Contracts.Models.Sagas;

namespace DistributedOrderSaga.Orchestration.Repositories;

public class SagaStateRepository(ILogger<SagaStateRepository> logger)
{
    private readonly ConcurrentDictionary<Guid, SagaState> _states = new();

    public async Task<SagaState?> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        return _states.GetValueOrDefault(orderId);
    }

    public async Task<IEnumerable<SagaState>> GetAllAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        return _states.Values.OrderByDescending(s => s.StartedAt);
    }

    public async Task<IEnumerable<SagaState>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        return _states.Values
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.StartedAt);
    }

    public async Task<bool> ExistsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        return _states.ContainsKey(orderId);
    }

    public async Task SaveAsync(SagaState state, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        if (!_states.TryAdd(state.Order.Id, state))
        {
            logger.LogWarning("SAGA state for OrderId={OrderId} already exists", state.Order.Id);
            return;
        }

        logger.LogInformation("Saved SAGA state for OrderId={OrderId}", state.Order.Id);
    }

    public async Task UpdateAsync(SagaState state, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        if (!_states.ContainsKey(state.Order.Id))
        {
            logger.LogWarning("SAGA state not found for OrderId={OrderId}", state.Order.Id);
            return;
        }

        _states[state.Order.Id] = state;
        logger.LogDebug("Updated SAGA state for OrderId={OrderId}", state.Order.Id);
    }

    public async Task<SagaStatistics> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        var states = (await GetAllAsync(cancellationToken)).ToList();
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