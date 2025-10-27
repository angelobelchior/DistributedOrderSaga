using DistributedOrderSaga.ServiceDefaults;
using DistributedOrderSaga.PaymentService.Consumers;
using DistributedOrderSaga.Messaging;
using DistributedOrderSaga.PaymentService.Repositories;
using DistributedOrderSaga.PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient(connectionName: "messaging");
builder.Services.AddRabbitMQExtensions();
builder.Services.AddHostedService<ProcessPaymentConsumer>();
builder.Services.AddHostedService<RefundPaymentConsumer>();
builder.Services.AddSingleton<PaymentRepository>();
builder.Services.AddSingleton<PaymentGatewayService>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.MapGet("/", () =>
    Results.Ok("Hello from Payment Service!"));

app.Run();