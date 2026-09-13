using FatControllerExample.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Npgsql;

namespace FatControllerExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FatOrderController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<FatOrderController> _logger;
        private readonly IConfiguration _configuration;

        public FatOrderController(
            IConfiguration configuration,
            ILogger<FatOrderController> logger)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder(OrderRequest req)
        {
            if (req.Items.Count == 0)
                return BadRequest("Заказ должен содержать хотя бы один товар");

            if (string.IsNullOrEmpty(req.CustomerEmail))
                return BadRequest("Email обязателен");

            // работа с БД в контроллере
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                decimal totalAmount = 0;

                // бизнес-логика в контроллере, в блоке транзакции
                foreach (var item in req.Items)
                {
                    totalAmount += item.Price * item.Quantity;
                }

                decimal discount = 0;
                if (!string.IsNullOrEmpty(req.PromoCode))
                {
                    using var checkCmd = new NpgsqlCommand(
                        "SELECT discount_percent FROM promo_codes WHERE code = @code AND is_active = true AND valid_until > @now",
                        conn, transaction);
                    checkCmd.Parameters.AddWithValue("@code", req.PromoCode);
                    checkCmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

                    var result = await checkCmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        int discountPercent = Convert.ToInt32(result);
                        discount = totalAmount * discountPercent / 100;
                    }
                }

                decimal finalAmount = totalAmount - discount;

                using var orderCmd = new NpgsqlCommand(
                    @"INSERT INTO orders (customer_name, customer_email, total_amount, discount, final_amount, status, created_at) 
                      VALUES (@customer_name, @customer_email, @total_amount, @discount, @final_amount, 'Pending', @created_at)
                      RETURNING id",
                    conn, transaction);

                orderCmd.Parameters.AddWithValue("@customer_name", req.CustomerName);
                orderCmd.Parameters.AddWithValue("@customer_email", req.CustomerEmail);
                orderCmd.Parameters.AddWithValue("@total_amount", totalAmount);
                orderCmd.Parameters.AddWithValue("@discount", discount);
                orderCmd.Parameters.AddWithValue("@final_amount", finalAmount);
                orderCmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow);

                var orderId = (int)(await orderCmd.ExecuteScalarAsync() ?? 0);

                foreach (var item in req.Items)
                {
                    using var itemCmd = new NpgsqlCommand(
                        @"INSERT INTO order_items (order_id, product_id, product_name, price, quantity) 
                          VALUES (@order_id, @product_id, @product_name, @price, @quantity)",
                        conn, transaction);

                    itemCmd.Parameters.AddWithValue("@order_id", orderId);
                    itemCmd.Parameters.AddWithValue("@product_id", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@product_name", item.ProductName);
                    itemCmd.Parameters.AddWithValue("@price", item.Price);
                    itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                foreach (var item in req.Items)
                {
                    using var updateCmd = new NpgsqlCommand(
                        "UPDATE products SET stock_quantity = stock_quantity - @quantity WHERE id = @product_id",
                        conn, transaction);

                    updateCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                    updateCmd.Parameters.AddWithValue("@product_id", item.ProductId);

                    await updateCmd.ExecuteNonQueryAsync();
                }

                // работа с почтой в контроллере
                await SendConfirmationEmail(req.CustomerEmail, req.CustomerName, orderId, finalAmount);

                _logger.LogInformation("Заказ {OrderId} создан для {CustomerEmail}. Сумма: {FinalAmount}",
                    orderId, req.CustomerEmail, finalAmount);

                await transaction.CommitAsync();

                return Ok(new OrderResponse
                {
                    OrderId = orderId,
                    TotalAmount = totalAmount,
                    Discount = discount,
                    FinalAmount = finalAmount,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Ошибка при создании заказа для {CustomerEmail}", req.CustomerEmail);

                using var errorConn = new NpgsqlConnection(_connectionString);
                await errorConn.OpenAsync();

                using var errorCmd = new NpgsqlCommand(
                    "INSERT INTO error_logs (error_message, customer_email, created_at) VALUES (@message, @email, @now)",
                    errorConn);

                errorCmd.Parameters.AddWithValue("@message", ex.Message);
                errorCmd.Parameters.AddWithValue("@email", req.CustomerEmail);
                errorCmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

                await errorCmd.ExecuteNonQueryAsync();

                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        // логика работы с емайл в контроллере
        private async Task SendConfirmationEmail(string email, string customerName, int orderId, decimal amount)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Магазин", emailSettings["SenderEmail"]));
            message.To.Add(new MailboxAddress(customerName, email));
            message.Subject = $"Подтверждение заказа #{orderId}";

            message.Body = new TextPart("html")
            {
                Text = $@"<h1>Заказ #{orderId} подтвержден</h1>"
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