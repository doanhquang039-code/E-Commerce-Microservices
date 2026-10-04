using ECommerce.Shared.Events;
using MailKit.Net.Smtp;
using MimeKit;

namespace ECommerce.Notification.API.Consumers;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(OrderCreatedEvent order);
    Task SendOrderCancellationAsync(OrderCancelledEvent order);
    Task SendOrderShippedAsync(OrderShippedEvent order);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(OrderCreatedEvent order)
    {
        var subject = $"✅ Xác nhận đơn hàng #{order.OrderId.ToString()[..8].ToUpper()}";
        var body = $"""
            <h2>Cảm ơn bạn đã đặt hàng!</h2>
            <p>Đơn hàng của bạn đã được xác nhận.</p>
            <p><strong>Mã đơn hàng:</strong> {order.OrderId}</p>
            <p><strong>Tổng tiền:</strong> {order.TotalAmount:N0} VNĐ</p>
            <p><strong>Địa chỉ giao hàng:</strong> {order.ShippingAddress}</p>
            <h3>Sản phẩm:</h3>
            <ul>
            {string.Join("", order.Items.Select(i => $"<li>{i.ProductName} x{i.Quantity} — {i.UnitPrice:N0} VNĐ</li>"))}
            </ul>
            """;

        await SendEmailAsync(order.UserEmail, subject, body);
    }

    public async Task SendOrderCancellationAsync(OrderCancelledEvent order)
    {
        var subject = $"❌ Đơn hàng #{order.OrderId.ToString()[..8].ToUpper()} đã bị hủy";
        var body = $"""
            <h2>Đơn hàng của bạn đã bị hủy</h2>
            <p><strong>Mã đơn hàng:</strong> {order.OrderId}</p>
            <p><strong>Lý do:</strong> {order.Reason}</p>
            <p>Nếu bạn đã thanh toán, số tiền sẽ được hoàn trả trong 3-5 ngày làm việc.</p>
            """;

        await SendEmailAsync(order.UserEmail, subject, body);
    }

    public async Task SendOrderShippedAsync(OrderShippedEvent order)
    {
        var subject = $"🚚 Đơn hàng #{order.OrderId.ToString()[..8].ToUpper()} đang được giao";
        var body = $"""
            <h2>Đơn hàng của bạn đang được vận chuyển!</h2>
            <p><strong>Mã đơn hàng:</strong> {order.OrderId}</p>
            <p><strong>Mã vận đơn:</strong> {order.TrackingNumber}</p>
            <p><strong>Đơn vị vận chuyển:</strong> {order.CarrierName}</p>
            """;

        await SendEmailAsync(order.UserEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var smtpSettings = _config.GetSection("Smtp");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ECommerce Shop", smtpSettings["From"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpSettings["Host"],
                int.Parse(smtpSettings["Port"] ?? "587"), false);
            await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
