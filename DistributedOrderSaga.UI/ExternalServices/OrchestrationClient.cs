using System.Text.Json;
using System.Text.Json.Serialization;
using DistributedOrderSaga.Contracts.Models.Sagas;

namespace DistributedOrderSaga.UI.ExternalServices;

public class OrchestrationClient(HttpClient client)
{
    private JsonSerializerOptions GetJsonOptions()
    {
        var jsonOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        return jsonOptions;
    }

    public async Task<SagaStatistics> GetStatisticsAsync()
    {
        var response = await client.GetAsync("api/v1/saga/statistics");
        if (!response.IsSuccessStatusCode) return new();
        var json = await response.Content.ReadAsStringAsync();
        var statistics = JsonSerializer.Deserialize<SagaStatistics>(json, GetJsonOptions());
        return statistics ?? new();
    }

    public async Task<IReadOnlyCollection<SagaState>> ListAllSagasAsync()
    {
        var response = await client.GetAsync("api/v1/saga");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        var sagas = JsonSerializer.Deserialize<List<SagaState>>(json, GetJsonOptions());
        return sagas ?? [];
    }

    public async Task<SagaState?> GetSagaAsync(Guid orderId)
    {
        var response = await client.GetAsync($"api/v1/saga/{orderId}");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        var saga = JsonSerializer.Deserialize<SagaState>(json, GetJsonOptions());
        return saga;
    }
}