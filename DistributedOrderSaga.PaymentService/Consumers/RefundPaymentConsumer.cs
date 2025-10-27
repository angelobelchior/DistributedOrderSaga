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

public class RefundPaymentConsumer(
    IConnection connection,
    PaymentRepository paymentRepository,
    PaymentGatewayService paymentGatewayService,
    BaseMessageConsumer messageConsumer,
    Publisher publisher, 
    ILogger<RefundPaymentConsumer> logger)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        _channel.QueueDeclareWithDLQ("refund_payment");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: nameof(RefundPaymentConsumer),
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var command = ea.Body.ToMessage<RefundPaymentCommand>();
                    var payment = paymentRepository.GetByOrderId(command.Order.Id);
                    if (payment is null)
                    {
                        var paymentNotFoundEvent = PaymentRefundFailedEvent.Create(
                            command.Order, 
                            $"Payment not found for Order Id {command.Order.Id}");
                        await publisher.PublishAsync("payment_refund_failed", paymentNotFoundEvent, ct);
                        logger.LogWarning("Payment not found for Order Id {OrderId}", command.Order.Id);
                        return;
                    }
                    
                    if (!payment.IsAlreadyProcessed(PaymentStatus.Refunded))
                    {
                        var result = paymentGatewayService.ProcessRefund(command.Order);
                        if (result.Status is PaymentStatus.Failed)
                        {
                            var errorMessage = !string.IsNullOrEmpty(result.ErrorMessage)
                                ? result.ErrorMessage
                                : $"Payment not refunded for Order Id {command.Order.Id}";
                            
                            var paymentRefundFailed = PaymentRefundFailedEvent.Create(
                                command.Order,
                                errorMessage);
                            await publisher.PublishAsync("payment_refund_failed", paymentRefundFailed, ct);
                            logger.LogWarning("Payment not refunded for Order Id {OrderId}", command.Order.Id);
                            return;
                        }

                        payment = payment.ChangeStatus(PaymentStatus.Refunded);
                        paymentRepository.Update(payment);
                        logger.LogInformation("Payment refunded for Order Id {OrderId}", command.Order.Id);
                    }

                    var paymentRefunded = PaymentRefundedEvent.Create(command.Order);
                    await publisher.PublishAsync("payment_refunded", paymentRefunded, ct);
                },
                stoppingToken,
                sendToDlq: true);
        };

        _channel.BasicConsume(queue: "refund_payment", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}