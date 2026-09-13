using FatControllerExample.Models;

namespace FatControllerExample.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<Order> CreateAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
        Task<PromoCode?> GetPromoCodeAsync(string code);
        Task UpdateProductStockAsync(int productId, int quantityChange);
    }

    public interface IErrorLogRepository
    {
        Task LogErrorAsync(string message, string email);
    }
}