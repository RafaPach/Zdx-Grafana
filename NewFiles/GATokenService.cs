using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using NOCAPI.Plugins.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx.NewFiles
{
    public class GATokenService
    {
        private readonly ILogger<GATokenService> _logger;

        public GATokenService(ILogger<GATokenService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {

            var serviceAccountPath = PluginConfigWrapper.Get("ServiceAccountJsonPath");

            //var serviceAccountPath = "C:\\nocapi\\Plugins\\NOCAPI.Modules.Zdx\\top10apps-480011-45ced6e29d3f.json";

            Console.WriteLine($"DOES SERVICE ACCOUNT FILE EXIST {File.Exists(serviceAccountPath)}");

            GoogleCredential credential;
            using (var stream = new FileStream(serviceAccountPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped("https://www.googleapis.com/auth/analytics.readonly");
            }

            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            return token;
        }
    }
}
