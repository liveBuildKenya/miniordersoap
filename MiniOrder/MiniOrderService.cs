using MiniOrder.Models;
using MiniOrder.OrderService;
using MiniOrder.ShippingService;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MiniOrder
{
    public static class MiniOrderService
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Initializing the application......");
            MiniOrderServiceCollection miniOrderServiceCollection = ConfigureServices();

            Console.WriteLine();
            Console.WriteLine("Application Started.");

            Console.WriteLine();
            Console.WriteLine("Getting the pending orders......");
            Console.WriteLine("-----------------------------------------------------------------------------------------------");
            OrderInfo[] orderInfos = await GetOrdersAsync(miniOrderServiceCollection.OrderServiceSoapClient);
            Console.WriteLine($"There are {orderInfos.Length} pending orders");

            Console.WriteLine();
            Console.WriteLine("How many orders should we process?");
            int ordersToProcess = Convert.ToInt32(Console.ReadLine());
            await ProcessOrders(ordersToProcess, orderInfos, miniOrderServiceCollection.OrderServiceSoapClient, miniOrderServiceCollection.ShippingServiceSoapClient);
        }

        /// <summary>
        /// Processes the orders
        /// </summary>
        /// <param name="ordersToProcess">number of orders to process</param>
        /// <param name="orders">A List of orders</param>
        /// <param name="orderServiceSoapClient">Order service soap client</param>
        /// <param name="shippingServiceSoapClient">Shipping service soap client</param>
        /// <returns></returns>
        public static async Task ProcessOrders(int ordersToProcess, OrderInfo[] orders, OrderServiceSoapClient orderServiceSoapClient, ShippingServiceSoapClient shippingServiceSoapClient)
        {
            Console.WriteLine();
            Console.WriteLine("Processing orders....");
            for (int orderInfoIndex = 0; orderInfoIndex < ordersToProcess; orderInfoIndex++)
            {
                Console.WriteLine($"##################################################################################");
                Console.WriteLine($"Customer Name: {orders[orderInfoIndex].CustomerName}");
                Console.WriteLine($"Order Date: {orders[orderInfoIndex].OrderDate}");
                Console.WriteLine($"Order Details");
                Console.WriteLine($"---------------------------------------");

                OrderInProcess currentOrder = await DisplayOrderBeingProcessed(orders, orderInfoIndex, orderServiceSoapClient);

                Console.WriteLine();
                Console.WriteLine($"Order Total: {currentOrder.TotalPrice}");

                Console.WriteLine();
                Console.WriteLine($"Printing the Labels......");
                LabelResult labelResult = await ProcessLabels(currentOrder.Order, currentOrder.Products, shippingServiceSoapClient);

                Console.WriteLine();
                Console.WriteLine($"Updating the order tracking number......");
                await UpdateTrackingNumber(currentOrder.Order, labelResult, orderServiceSoapClient);
                
                Console.WriteLine();
                Console.WriteLine($"##################################################################################");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Displays the order currently being processed
        /// </summary>
        /// <param name="orders">Orders</param>
        /// <param name="orderInfoIndex">Order info index</param>
        /// <param name="orderServiceSoapClient">Order service SOAP client</param>
        /// <returns></returns>
        public static async Task<OrderInProcess> DisplayOrderBeingProcessed(OrderInfo[] orders, int orderInfoIndex, OrderServiceSoapClient orderServiceSoapClient)
        {
            Order order = await orderServiceSoapClient.GetOrderAsync(orders[orderInfoIndex].OrderId);

            ShippingService.Product[] products = new ShippingService.Product[order.OrderDetails.Length];

            decimal? totalPrice = 0;
            for (int orderDetailIndex = 0; orderDetailIndex < order.OrderDetails.Length; orderDetailIndex++)
            {
                Console.WriteLine($"Product Name: {order.OrderDetails[orderDetailIndex].Products.Name} @ {order.OrderDetails[orderDetailIndex].Products.Price}");
                Console.WriteLine($"Product Description: {order.OrderDetails[orderDetailIndex].Products.Description}");
                Console.WriteLine();
                products[orderDetailIndex] = new ShippingService.Product
                {
                    Name = order.OrderDetails[orderDetailIndex].Products.Name,
                    Description = order.OrderDetails[orderDetailIndex].Products.Description,
                    Price = order.OrderDetails[orderDetailIndex].Products.Price
                };

                totalPrice += order.OrderDetails[orderDetailIndex].Products.Price;
            }

            return new OrderInProcess
            {
                Order = order,
                Products = products,
                TotalPrice = totalPrice
            };
        }

        /// <summary>
        /// Updates the tracking number of the application
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="labelResult">Label result</param>
        /// <param name="orderServiceSoapClient">Order service SOAP client</param>
        public static async Task UpdateTrackingNumber(Order order, LabelResult labelResult, OrderServiceSoapClient orderServiceSoapClient)
        {
            Console.WriteLine($"{await orderServiceSoapClient.UpdateTrackingNumberAsync(order.OrderId, labelResult.TrackingNumber)} The new tracking number for this order is {labelResult.TrackingNumber}");
        }

        /// <summary>
        /// Processes the label of the order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="products">List of Products</param>
        /// <param name="shippingServiceSoapClient">Shipping service SOAP client</param>
        /// <returns>Label Result and prints labels to a labels folder of the projects directory</returns>
        public static async Task<LabelResult> ProcessLabels(Order order, ShippingService.Product[] products, ShippingServiceSoapClient shippingServiceSoapClient)
        {
            LabelInformation labelInformation = new LabelInformation()
            {
                Customer = new ShippingService.Customer
                {
                    FirstName = order.Customer.FirstName,
                    LastName = order.Customer.LastName,
                    Email = order.Customer.Email,
                    BillingAddress = new ShippingService.Address
                    {
                        City = order.Customer.BillingAddress.City,
                        State = order.Customer.BillingAddress.State,
                        Street1 = order.Customer.BillingAddress.Street1,
                        Street2 = order.Customer.BillingAddress.Street2,
                        Zip = order.Customer.BillingAddress.Zip
                    },
                    ShippingAddress = new ShippingService.Address
                    {
                        City = order.Customer.ShippingAddress.City,
                        State = order.Customer.ShippingAddress.State,
                        Street1 = order.Customer.ShippingAddress.Street1,
                        Street2 = order.Customer.ShippingAddress.Street2,
                        Zip = order.Customer.ShippingAddress.Zip
                    }
                },

                Products = products
            };
            LabelResult labelResult = await shippingServiceSoapClient.GetLabelAsync(labelInformation);

            File.WriteAllText($"../../../Labels/{labelResult.TrackingNumber}.html", labelResult.Html);
            Console.WriteLine($"Label for order with tracking number {labelResult.TrackingNumber} printed.");

            return labelResult;
        }

        /// <summary>
        /// Gets the orders
        /// </summary>
        /// <param name="orderServiceSoapClient">The Order service SOAP client</param>
        /// <returns>A list of Orders</returns>
        public static async Task<OrderInfo[]> GetOrdersAsync(OrderServiceSoapClient orderServiceSoapClient)
        {
            OrderInfo[] orderInfos = await orderServiceSoapClient.GetOrdersAsync();
            foreach(OrderInfo orderInfo in orderInfos)
            {
                Console.WriteLine($"Customer Name: {orderInfo.CustomerName} - {orderInfo.OrderDate}");
            }

            return orderInfos;
        }

        /// <summary>
        /// Configures the SOAP Services to be used by the application
        /// </summary>
        /// <returns>MiniOrderServiceCollection object</returns>
        public static MiniOrderServiceCollection ConfigureServices()
        {
            return new MiniOrderServiceCollection
            {
                OrderServiceSoapClient = new OrderServiceSoapClient(OrderServiceSoapClient.EndpointConfiguration.OrderServiceSoap),
                ShippingServiceSoapClient = new ShippingServiceSoapClient(ShippingServiceSoapClient.EndpointConfiguration.ShippingServiceSoap)
            };
        }
    }
}
