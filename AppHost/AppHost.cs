var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);

var rabbitmq = builder.AddRabbitMQ("messaging", username, password)
        .WithManagementPlugin(port: 15672)
        //.WithDataVolume("rabbitmq_pvc")
        .WithOtlpExporter()
    ;

builder.AddProject<Projects.DistributedOrderSaga_Orchestration>("orchestration")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithOtlpExporter()
    ;

builder.AddProject<Projects.DistributedOrderSaga_OrderService>("orders")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithOtlpExporter()
    ;

builder.AddProject<Projects.DistributedOrderSaga_PaymentService>("payment")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithOtlpExporter()
    ;

builder.AddProject<Projects.DistributedOrderSaga_ShippingService>("shipping")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithOtlpExporter()
    ;

builder.AddProject<Projects.DistributedOrderSaga_UI>("ui")
    .WithOtlpExporter()
    ;

builder.Build().Run();