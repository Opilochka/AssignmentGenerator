using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Opilochka.Core.Auth;
using Opilochka.Core.ViewModels;


namespace Opilochka.Web.Services.Auth
{
    public class AuthService
    {
        private readonly NavigationManager _navigationManager;
        private readonly AccessTokenService _tokenService;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            NavigationManager navigationManager,
            AccessTokenService tokenService,
            RefreshTokenService refreshTokenService,
            IHttpClientFactory httpClient,
            ILogger<AuthService> logger)
        {
            _navigationManager = navigationManager;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _httpClient = httpClient.CreateClient("ApiClient");
            _logger = logger;
        }

        public async Task<bool> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Email или пароль пустые или содержат только пробелы.");
                return false;
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("Auth/login", new AuthRequest { Email = email, Password = password });

                if (response.IsSuccessStatusCode)
                {
                    var token = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AuthResponse>(token);

                    if (result != null && !string.IsNullOrEmpty(result.AccessToken))
                    {
                        await _tokenService.SetToken(result.AccessToken);
                        await _refreshTokenService.Set(result.RefreshToken);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Получен некорректный ответ аутентификации.");
                    }
                }
                else
                {
                    _logger.LogWarning("Не удалось войти. Код статуса: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время процесса входа в систему.");
            }

            return false; 
        }
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await _refreshTokenService.Get();

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    _logger.LogWarning("Не удалось получить refresh token");
                    return false;
                }

                _httpClient.DefaultRequestHeaders.Add("Cookie", $"refreshtoken={refreshToken}");
                var responseMessage = await _httpClient.PostAsync("Auth/refresh", null);

                if (responseMessage.IsSuccessStatusCode)
                {
                    var tokenContent = await responseMessage.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(tokenContent))
                    {
                        var result = JsonConvert.DeserializeObject<AuthResponse>(tokenContent);

                        if (result != null)
                        {
                            await _tokenService.RemoveToken();
                            await _tokenService.SetToken(result.AccessToken);
                            await _refreshTokenService.Set(result.RefreshToken);
                            _logger.LogInformation("Токены успешно обновлены.");
                            return true;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Ошибка при обновлении токена. Код состояния: {responseMessage.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при обновлении токена: {Message}", ex.Message);
            }

            return false;
        }
        public async Task Logout()
        {
            try
            {
                var refreshToken = await _refreshTokenService.Get();
                _httpClient.DefaultRequestHeaders.Add("Cookie", $"refreshtoken={refreshToken}");
                var responseMessage = await _httpClient.PostAsync("Auth/logout", null);

                if (responseMessage.IsSuccessStatusCode)
                {
                    await _tokenService.RemoveToken();
                    await _refreshTokenService.Remove(); 
                    _logger.LogInformation("Пользователь успешно вышел из системы");
                    _navigationManager.NavigateTo("/login", forceLoad: true);
                }
                else
                {
                    _logger.LogWarning($"Ошибка при выходе пользователя. Код состояния: {responseMessage.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при попытке выхода из системы: {Message}", ex.Message);
            }
        }
    }
}
