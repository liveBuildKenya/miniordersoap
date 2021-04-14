using System.Threading.Tasks;

namespace MiniOrder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await MiniOrderService.RunAsync();
        }

    }
}
