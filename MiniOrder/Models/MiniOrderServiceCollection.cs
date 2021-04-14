using MiniOrder.OrderService;
using MiniOrder.ShippingService;

namespace MiniOrder.Models
{
    /// <summary>
    /// Represents the MiniOrder application service collection
    /// </summary>
    public class MiniOrderServiceCollection
    {
        public OrderServiceSoapClient OrderServiceSoapClient { get; set; }
        public ShippingServiceSoapClient ShippingServiceSoapClient { get; set; }
    }
}
