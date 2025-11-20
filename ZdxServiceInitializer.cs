using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NOCAPI.Modules.Zdx.NewFiles;
using NOCAPI.Modules.Zdx.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx
{
    public class ZdxServiceInitializer
    {
        private static readonly object _lock = new();
        private static bool _initialized = false;

        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                var services = new ServiceCollection();

                // Core services
                services.AddMemoryCache();
                services.AddScoped<PocHelper>();
                services.AddSingleton<TokenService>();
                services.AddSingleton<RateLimiter>();

                // HTTP Client
                services.AddHttpClient("Default")
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new HttpClientHandler
                        {
                            UseProxy = true,
                            Proxy = WebRequest.GetSystemWebProxy(),
                            ServerCertificateCustomValidationCallback =
                                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        });

                // If you want metrics background refresh:
                services.AddHostedService<ZdxBackgroundService>();

                // Build container
                ServiceProvider = services.BuildServiceProvider();

                // Start hosted services (ONLY because you do not have Program.cs)
                foreach (var hosted in ServiceProvider.GetServices<IHostedService>())
                {
                    hosted.StartAsync(CancellationToken.None)
                          .GetAwaiter()
                          .GetResult();
                }

                _initialized = true;
            }
        }   

    }
}
