using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;

namespace DistributedOrderSaga.Messaging;

public class Publisher : IDisposable
{
    private readonly IModel _channel;
    private readonly ILogger<Publisher> _logger;
    private readonly Lock _lock = new();

    private readonly AsyncPolicy _retryPolicy;

    public Publisher(IConnection connection, ILogger<Publisher> logger)
    {
        _channel = connection.CreateModel();
        _channel.ConfirmSelect();

        _logger = logger;

        //TODO Deixar os valores configuráveis
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, _) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount}/3 after {Delay}s due to: {Message}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });
    }

    public Task PublishAsync<T>(string destination, T message, CancellationToken cancellationToken)
        => _retryPolicy.ExecuteAsync(ct =>
        {
            ct.ThrowIfCancellationRequested();

            lock (_lock)
            {
                using var activity =
                    Constants.ActivitySource.StartActivity($"Publish {destination}", ActivityKind.Producer);

                var props = _channel.CreateBasicProperties();
                props.Persistent = true;
                props.ContentType = "application/json";
                props.MessageId = Guid.NewGuid().ToString();
                if (Activity.Current != null)
                {
                    props.Headers = new ActivityTagsCollection();
                    props.Headers["traceparent"] = Activity.Current.Id;
                }

                var body = JsonSerializer.SerializeToUtf8Bytes(message);
                _channel.BasicPublish(exchange: "", routingKey: destination, basicProperties: props, body: body);
                //TODO: deixar o tempo configurável
                if (!_channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
                {
                    _logger.LogError("Failed to confirm message to '{destination}'", destination);
                    throw new InvalidOperationException($"Message to {destination} was not confirmed by broker");
                }

                _logger.LogInformation("Published to '{destination}' (persistent, confirmed): {message}", destination,
                    message);
            }

            return Task.CompletedTask;
        }, cancellationToken);

    public void Dispose()
        => _channel.Dispose();
}