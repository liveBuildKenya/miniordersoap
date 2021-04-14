using MiniOrder.OrderService;

namespace MiniOrder.Models
{
    /// <summary>
    /// Represents the current order in process
    /// </summary>
    public class OrderInProcess
    {
        /// <summary>
        /// Gets or sets the order
        /// </summary>
        public Order Order { get; set; }

        /// <summary>
        /// Gets or sets the products
        /// </summary>
        public ShippingService.Product[] Products { get; set; }
        
        /// <summary>
        /// Gets or sets the order total price
        /// </summary>
        public decimal? TotalPrice { get; set; }
    }
}
