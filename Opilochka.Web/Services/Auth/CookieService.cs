using Microsoft.JSInterop;

namespace Opilochka.Web.Services.Auth
{
    public class CookieService
    {
        private readonly IJSRuntime _runtime;
        private readonly ILogger<CookieService> _logger;

        public CookieService(IJSRuntime runtime, ILogger<CookieService> logger)
        {
            _runtime = runtime;
            _logger = logger;
        }

        public async Task<string?> Get(string key)
        {
            try
            {
                var value = await _runtime.InvokeAsync<string>("getCookie", key);
                _logger.LogInformation("Получено значение куки по ключу: {Key} - значение: {Value}", key, value);
                return value;
            }
            catch (Exception ex)
            {
                //_logger.LogError("Ошибка при получении куки по ключу: {Key}. Сообщение об ошибке: {ErrorMessage}", key, ex.Message);
                return null;
            }
        }

        public async Task Remove(string key)
        {
            try
            {
                await _runtime.InvokeVoidAsync("deleteCookie", key);
                _logger.LogInformation("Кука удалена по ключу: {Key}", key);
            }
            catch (Exception ex)
            {
                //_logger.LogError("Ошибка при удалении куки по ключу: {Key}. Сообщение об ошибке: {ErrorMessage}", key, ex.Message);
            }
        }

        public async Task Set(string key, string value, int days)
        {
            try
            {
                await _runtime.InvokeVoidAsync("setCookie", key, value, days);
                _logger.LogInformation("Кука установлена по ключу: {Key} - значение: {Value}, срок действия: {Days} дней", key, value, days);
            }
            catch (Exception ex)
            {
                //_logger.LogError("Ошибка при установке куки по ключу: {Key}. Сообщение об ошибке: {ErrorMessage}", key, ex.Message);
            }
        }
    }
}
