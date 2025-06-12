using Microsoft.Extensions.Logging;
using Opilochka.Data.Answers;
using Opilochka.Data.Tasks;
using Opilochka.Data.Users;
using Opilochka.Web.Services.Admin;
using Opilochka.Web.Services.Auth;

namespace Opilochka.Web.Services.Teacher
{
    public class ResultCheckService(
        ILogger<ResultCheckService> logger,
        HttpRequestHandler httpRequestHandler,
        APIService aPIService)
    {
        private readonly ILogger<ResultCheckService> _logger = logger;
        private readonly HttpRequestHandler _httpRequestHandler = httpRequestHandler;
        private readonly APIService _service = aPIService;

        public async Task<List<ResultCheck>?> GetResultChecksAsync()
        {
            _logger.LogInformation("Вызван метод ResultChecks");

            var resultCheck = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync("ResultCheck");
                return await _httpRequestHandler.HandleResponseAsync<List<ResultCheck>>(response);
            });

            if (resultCheck == null || resultCheck.Count == 0)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но список проверок пуст");
            }
            else
            {
                _logger.LogInformation("Получено {resultCheckCount} проверок", resultCheck.Count);
            }

            return resultCheck;
        }

        public async Task<User?> CreateResultCheckAsync(Data.Tasks.Task task, Answer answer)
        {
            PostRequestResultCheck postRequestResultCheck = new()
            {
                Answer = answer,
                Task = task
            };
            _logger.LogInformation("Вызван метод CreateResultCheck");

            var user = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PostAsync("ResultCheck", postRequestResultCheck);
                _logger.LogInformation("Запрос на создание проверки задания. Статус ответа: {StatusCode}", response.StatusCode);

                var userResponse = await _httpRequestHandler.HandleResponseAsync<User>(response);
                return userResponse;
            });

            if (user != null)
            {
                _logger.LogInformation("Оценка успешно создан: {@User}", user);
            }
            else
            {
                _logger.LogWarning("Создание оценки завершилось неудачно. Ответ от API: null");
            }

            return user;
        }

        public async Task<ResultCheck?> UpdateResultCheckAsync(long id, ResultCheck request)
        {
            if (request == null)
            {
                _logger.LogError("Ошибка: Запрос не может быть null для обновления проверки с идентификатором: {ResultId}", id);
                throw new ArgumentNullException(nameof(request), "Запрос не может быть null");
            }

            _logger.LogInformation("Вызван метод UpdateResultCheckAsync для проверки с идентификатором: {ResultId} с данными: {@Request}", id, request);

            return await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PutAsync($"ResultCheck/{id}", request);
                _logger.LogInformation("Запрос на обновление проверки с идентификатором: {ResultId} отправлен. Статус ответа: {StatusCode}", id, response.StatusCode);

                var resultCheck = await _httpRequestHandler.HandleResponseAsync<ResultCheck>(response, id);

                if (resultCheck != null)
                {
                    _logger.LogInformation("Оценка с идентификатором: {resultCheckId} успешно обновлена: {@resultCheck}", id, resultCheck);
                }
                else
                {
                    _logger.LogWarning("Обновление оценки с идентификатором: {resultCheckId} завершилось неудачно. Ответ от API: null.", id);
                }

                return resultCheck;
            });
        }
    }
}
