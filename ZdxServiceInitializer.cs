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

                services.AddMemoryCache();

                services.AddScoped<PocHelper>();

                services.AddSingleton<TokenService>();

                services.AddSingleton<RateLimiter>();

                //services.AddHostedService<MetricsBackgroundService>();

                //services.AddHostedService<MetricsBS>();

                services.AddHttpClient("Default").ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = WebRequest.GetSystemWebProxy(),
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });

                ServiceProvider = services.BuildServiceProvider();


                var hostedServices = ServiceProvider.GetServices<IHostedService>();
                foreach (var hostedService in hostedServices)
                {
                    hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
                }


                _initialized = true;
            }
        }   

    }
}
