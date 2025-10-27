using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Payments;
using DistributedOrderSaga.Contracts.Events.Orders;
using DistributedOrderSaga.Orchestration.Models;
using DistributedOrderSaga.Orchestration.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;

namespace DistributedOrderSaga.Orchestration.Consumers;

public class OrderCreatedConsumer(
    IConnection connection,
    BaseMessageConsumer messageConsumer,
    Publisher publisher,
    SagaStateRepository sagaStateRepository,
    SagaStateUpdater sagaStateUpdater)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        //TODO: Deixar prefetchCount configurável
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        _channel.QueueDeclareWithDLQ("order_created");
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: "OrderCreatedConsumer",
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var evt = ea.Body.ToMessage<OrderCreatedEvent>();

                    if (!sagaStateRepository.Exists(evt.Order.Id))
                    {
                        var state = SagaState.CreateFromOrder(evt.Order);
                        sagaStateRepository.Save(state);

                        sagaStateUpdater.TransitionToStatus(
                            state,
                            SagaStatus.AwaitingPayment,
                            SagaEvent.ProcessPayment);
                        
                        sagaStateRepository.Update(state);

                        var processPayment = ProcessPaymentCommand.Create(evt.Order);
                        await publisher.PublishAsync("process_payment", processPayment, ct);
                    }
                },
                stoppingToken,
                sendToDlq: true);
        };
        _channel.BasicConsume(queue: "order_created", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}