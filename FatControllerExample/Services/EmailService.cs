using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FatControllerExample.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOrderConfirmationAsync(string email, string customerName, int orderId, decimal amount)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Магазин", emailSettings["SenderEmail"]));
            message.To.Add(new MailboxAddress(customerName, email));
            message.Subject = $"Подтверждение заказа #{orderId}";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <h1>Заказ #{orderId} подтвержден</h1>
                <p>Здравствуйте, {customerName}!</p>
                <p>Ваш заказ на сумму {amount:C} успешно создан.</p>
                <p>Спасибо за покупку!</p>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                emailSettings["SmtpServer"],
                int.Parse(emailSettings["SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                emailSettings["SenderEmail"],
                emailSettings["SenderPassword"]);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}