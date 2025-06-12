namespace Opilochka.Web.Services
{
    public class HttpRequestHandler
    {
        private readonly ILogger<HttpRequestHandler> _logger;

        public HttpRequestHandler(ILogger<HttpRequestHandler> logger)
        {
            _logger = logger;
        }

        public async Task<T?> ExecuteAsync<T>(Func<Task<T?>> action)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Ошибка HTTP: {ex.Message}");
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Произошла неожиданная ошибка: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, long? id = null)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Запрос выполнен успешно для идентификатора: {id}");
                return await response.Content.ReadFromJsonAsync<T>();
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Ошибка при обработке запроса для идентификатора: {id}: {response.StatusCode} - {errorMessage}");
            throw new HttpRequestException($"Ошибка: {response.StatusCode}");
        }
    }
}
