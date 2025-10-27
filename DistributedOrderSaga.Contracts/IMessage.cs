using System.Text;
using System.Text.Json;

namespace DistributedOrderSaga.Contracts;

public interface IMessage
{
    public Guid Id { get; init; }
}

public interface ICommand : IMessage;

public interface IEvent : IMessage;

public static class MessageExtensions
{
    public static byte[] ToByteArray(this IMessage message)
        => JsonSerializer.SerializeToUtf8Bytes(message, message.GetType());
    
    public static T ToMessage<T>(this ReadOnlyMemory<byte> body)
    {
        var json = Encoding.UTF8.GetString(body.Span);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var message = JsonSerializer.Deserialize<T>(json, options);
        return message ??
               throw new InvalidOperationException(
                   $"Falha ao desserializar para o tipo {typeof(T).FullName}. JSON: {json}");
    }
}

