using Microsoft.Extensions.DependencyInjection;

namespace Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();

            var startup = new Startup();
            startup.ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Get Service and call method
            using (var scope = serviceProvider.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<ITransactionEventProcessor>();
                service.Process();
            }
        }
    }
}
