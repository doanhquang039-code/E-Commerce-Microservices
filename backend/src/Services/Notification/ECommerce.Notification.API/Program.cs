using ECommerce.Notification.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Background Kafka Consumer
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
