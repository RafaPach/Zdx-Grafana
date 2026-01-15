using Microsoft.Extensions.Caching.Memory;
using NOCAPI.Plugins.Config;
using System.Text.Json;

namespace NOCAPI.Modules.Zdx
{
    public class TokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private record CachedToken(string AccessToken, DateTime Expiry);

        public TokenService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            const string cacheKey = "ZDX_AccessToken";

            if (_cache.TryGetValue(cacheKey, out CachedToken cachedToken))
            {
                var remaining = cachedToken.Expiry - DateTime.UtcNow;
                Console.WriteLine($"Token still valid, expires in {remaining.TotalSeconds:F0}s");
                return cachedToken.AccessToken;
            }

            await _semaphore.WaitAsync();

            try
            {
                if (_cache.TryGetValue(cacheKey, out cachedToken))
                {
                    var remaining = cachedToken.Expiry - DateTime.UtcNow;
                    Console.WriteLine($"Token still valid (after semaphore), expires in {remaining.TotalSeconds:F0}s");
                    return cachedToken.AccessToken;
                }

                var tokenResponse = await FetchTokenAsync();

                var wrappedToken = new
                {
                    token = new
                    {
                        access_token = tokenResponse.Access_token,
                        token_type = tokenResponse.Token_type,
                        expires_in = tokenResponse.Expires_in
                    }
                };

                var json = JsonSerializer.Serialize(wrappedToken, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);

                var expiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.Expires_in - 60);

                _cache.Set(cacheKey, new CachedToken(tokenResponse.Access_token, expiryTime),
                           new MemoryCacheEntryOptions
                           {
                               AbsoluteExpiration = expiryTime
                           });

                return tokenResponse.Access_token;
            }

            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<TokenResponse> FetchTokenAsync()
        {
            var tokenUrl = PluginConfigWrapper.Get("TokenUrl");
            var clientId = PluginConfigWrapper.Get("ClientId");
            var clientSecret = PluginConfigWrapper.GetSecure("ClientSecret");
            var audience = PluginConfigWrapper.Get("Audience");

            Console.WriteLine($"Token URL = {tokenUrl}");
            if (string.IsNullOrWhiteSpace(tokenUrl))
                throw new Exception("Token URL not found in configuration!");

            var client = _httpClientFactory.CreateClient();

            var tokenRequestData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("audience", audience)
            });

            var response = await client.PostAsync(tokenUrl, tokenRequestData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get token: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenData == null || string.IsNullOrEmpty(tokenData.Access_token))
            {
                throw new Exception("Invalid token response");
            }

            return tokenData;
        }

        private class TokenResponse
        {
            public string Access_token { get; set; } = default!;
            public string Token_type { get; set; } = default!;
            public int Expires_in { get; set; }
        }
    }
}
