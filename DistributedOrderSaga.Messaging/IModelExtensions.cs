using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace DistributedOrderSaga.Messaging;

public static class IModelExtensions
{
    public static void DefaultQueueDeclare(this IModel model, string queue,
        IDictionary<string, object>? arguments = null)
        => model.QueueDeclare(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments);

    public static void QueueDeclareWithDLQ(this IModel model, string queue)
    {
        var dlq = $"{queue}_dlq";
        model.QueueDeclareWithDLQ(queue, dlq);
    }

    private static void QueueDeclareWithDLQ(this IModel model, string queue, string dlq)
    {
        var dlqArgs = new Dictionary<string, object>
        {
            //TODO Deixar o TTL configurado por queue
            { "x-message-ttl", TimeSpan.FromDays(7).Milliseconds },
            { "x-queue-mode", "lazy" }
        };
        model.DefaultQueueDeclare(dlq, dlqArgs);

        var arguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "" },
            { "x-dead-letter-routing-key", dlq }
        };

        try
        {
            model.DefaultQueueDeclare(queue, arguments);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 406)
        {
            
            //TODO Geralmente a criação das filas é feita via arquivo de configuração ou pelo time de infraestrutura.
            //Para fins didáticos, vamos tentar criar a fila aqui.
            try
            {
                model.QueueDelete(queue, ifUnused: true, ifEmpty: true);
                model.DefaultQueueDeclare(queue, arguments);
            }
            catch (Exception recreateEx)
            {
                throw new InvalidOperationException(
                    $"Queue '{queue}' exists with different arguments and could not be recreated automatically. " +
                    "Delete it manually (ensure it's empty and unused) or unset DLQ.", recreateEx);
            }
        }
    }
}