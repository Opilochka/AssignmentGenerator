using System.Net.Http.Headers;

namespace Opilochka.Web.Services.Auth
{
    public class APIService(AccessTokenService tokenService,
        AuthService authService,
        IHttpClientFactory httpClient,
        ILogger<APIService> logger)
    {
        private readonly AccessTokenService _tokenService = tokenService;
        private readonly AuthService _authService = authService;
        private readonly HttpClient _httpClient = httpClient.CreateClient("ApiClient");
        private readonly ILogger<APIService> _logger = logger;

        private async Task<HttpResponseMessage> SendRequestWithTokenAsync(Func<string, Task<HttpResponseMessage>> sendRequest, string endpoint)
        {
            var token = await _tokenService.GetToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation($"Отправка запроса к {endpoint} с токеном");

            var responseMessage = await sendRequest(endpoint);

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Токен недействителен, пытаемся обновить токен");

                var refreshTokenResult = await _authService.RefreshTokenAsync();
                if (!refreshTokenResult)
                {
                    _logger.LogError("Не удалось обновить токен, выполняем выход");
                    await _authService.Logout();
                }

                var newToken = await _tokenService.GetToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                _logger.LogInformation("Токен обновлён, повторяем запрос");
                responseMessage = await sendRequest(endpoint);
            }

            return responseMessage;
        }


        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await SendRequestWithTokenAsync(endpoint => _httpClient.GetAsync(endpoint), endpoint);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object obj)
        {
            return await SendRequestWithTokenAsync(endpoint => _httpClient.PostAsJsonAsync(endpoint, obj), endpoint);
        }

        public async Task<HttpResponseMessage> PutAsync(string endpoint, object obj)
        {
            return await SendRequestWithTokenAsync(endpoint => _httpClient.PutAsJsonAsync(endpoint, obj), endpoint);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            return await SendRequestWithTokenAsync(endpoint => _httpClient.DeleteAsync(endpoint), endpoint);
        }
    }
}
