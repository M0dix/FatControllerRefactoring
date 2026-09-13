using System.ComponentModel.DataAnnotations;

namespace FatControllerExample.DTOs
{
    public class OrderRequest
    {
        [Required(ErrorMessage = "Имя клиента обязательно")]
        [StringLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Список товаров обязателен")]
        [MinLength(1, ErrorMessage = "Заказ должен содержать хотя бы один товар")]
        public List<OrderItemRequest> Items { get; set; } = new();

        [StringLength(20, ErrorMessage = "Промокод не должен превышать 20 символов")]
        public string? PromoCode { get; set; }
    }

    public class OrderItemRequest
    {
        [Required(ErrorMessage = "ID товара обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "ID товара должен быть положительным числом")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Название товара обязательно")]
        [StringLength(100, ErrorMessage = "Название товара не должно превышать 100 символов")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1")]
        public int Quantity { get; set; }
    }

    public class OrderResponse
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}