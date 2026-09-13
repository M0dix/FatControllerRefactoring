namespace FatControllerExample.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public bool IsActive { get; set; }
        public DateTime ValidUntil { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
    }
}