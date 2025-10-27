using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Payments;
using DistributedOrderSaga.Contracts.Events.Payments;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;
using DistributedOrderSaga.PaymentService.Models;
using DistributedOrderSaga.PaymentService.Repositories;
using DistributedOrderSaga.PaymentService.Services;

namespace DistributedOrderSaga.PaymentService.Consumers;

public class ProcessPaymentConsumer(
    IConnection connection,
    PaymentRepository paymentRepository,
    PaymentGatewayService paymentGatewayService,
    BaseMessageConsumer messageConsumer,
    Publisher publisher,
    ILogger<ProcessPaymentConsumer> logger)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        //TODO: Deixar prefetchCount configurável
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        _channel.QueueDeclareWithDLQ("process_payment");
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: nameof(ProcessPaymentConsumer),
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var command = ea.Body.ToMessage<ProcessPaymentCommand>();

                    var payment = paymentRepository.GetByOrderId(command.Order.Id);

                    if (payment is not null && payment.IsAlreadyProcessed(PaymentStatus.Approved))
                    {
                        var paymentApproved = PaymentApprovedEvent.Create(command.Order);
                        await publisher.PublishAsync("payment_approved", paymentApproved, ct);
                        logger.LogInformation("Payment for order {OrderId} already approved", command.Order.Id);
                        return;
                    }

                    logger.LogInformation("Processing payment for order {OrderId}", command.Order.Id);
                    var result = paymentGatewayService.ProcessPayment(command.Order.Payment);
                    payment = Payment.Create(command.Order.Id, result.Status);
                    paymentRepository.Insert(payment);

                    if (result.Status is PaymentStatus.Declined or PaymentStatus.Failed)
                    {
                        var paymentRejected = PaymentRejectedEvent.Create(command.Order, result.ErrorMessage);
                        await publisher.PublishAsync("payment_rejected", paymentRejected, ct);
                        logger.LogInformation("Payment for order {OrderId} rejected. Status: {Status}",
                            command.Order.Id, result.Status);
                    }
                    else
                    {
                        var paymentApproved = PaymentApprovedEvent.Create(command.Order);
                        await publisher.PublishAsync("payment_approved", paymentApproved, ct);
                        logger.LogInformation("Payment for order {OrderId} approved.", command.Order.Id);
                    }
                },
                stoppingToken,
                sendToDlq: true);
        };
        _channel.BasicConsume(queue: "process_payment", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}