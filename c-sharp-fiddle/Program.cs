using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace c_sharp_fiddle
{
    class Program
    {
        static void Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<Models.ILogger, Models.Logger>();
                    services.AddTransient<Models.ITransientLogger, Models.Logger>();
                    services.AddTransient<Services.ILongRunningTask, Services.LongRunningTask>();
                    services.AddTransient<App>();
                });

            var host = hostBuilder.Build();
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                try
                {
                    var app = services.GetRequiredService<App>();
                    ServiceLocator.Initialize(services);
                    app.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}

