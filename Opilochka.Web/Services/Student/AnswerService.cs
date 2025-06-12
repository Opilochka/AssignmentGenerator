using Opilochka.Data.Answers;
using Opilochka.Data.Users;
using Opilochka.Web.Services.Auth;

namespace Opilochka.Web.Services.Student
{
    public class AnswerService(
        ILogger<AnswerService> logger,
        HttpRequestHandler httpRequestHandler,
        APIService aPIService)
    {
        private readonly ILogger<AnswerService> _logger = logger;
        private readonly HttpRequestHandler _httpRequestHandler = httpRequestHandler;
        private readonly APIService _service = aPIService;

        public async Task<List<Answer>?> GetAnswersAsync()
        {
            _logger.LogInformation("Вызван метод GetAnswers");

            var answers = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync("Answer");
                return await _httpRequestHandler.HandleResponseAsync<List<Answer>>(response);
            });

            if (answers == null || answers.Count == 0)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но список ответов пуст");
            }
            else
            {
                _logger.LogInformation("Получено {AnswerCount} ответов", answers.Count);
            }

            return answers;
        }

        public async Task<Answer?> GetAnswerAsync(long id)
        {
            _logger.LogInformation("Вызван метод GetAnswer для ответ с ID {AnswerId}", id);

            var answer = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync($"Answer/{id}");
                return await _httpRequestHandler.HandleResponseAsync<Answer>(response);
            });

            if (answer == null)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но ответ с ID {AnswerId} не найден", id);
            }
            else
            {
                _logger.LogInformation("Получен ответ: {@User}", answer);
            }

            return answer;
        }

        public async Task<Answer?> CreateAnswerAsync(string code, string compiler, long UserId, long TaskId)
        {
            Answer request = new()
            {
                TextCompiler = compiler,
                TextAnswer = code,
                TaskId = TaskId,
                UserId = UserId,
            };

            _logger.LogInformation("Вызван метод CreateAnswer с данными: {@Request}", request);

            var answer = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PostAsync("Answer", request);
                _logger.LogInformation("Запрос на создание ответа отправлен. Статус ответа: {StatusCode}", response.StatusCode);

                var userResponse = await _httpRequestHandler.HandleResponseAsync<Answer>(response);
                return userResponse;
            });

            if (answer != null)
            {
                _logger.LogInformation("Ответ успешно создан: {@Answer}", answer);
            }
            else
            {
                _logger.LogWarning("Создание пользователя завершилось неудачно. Ответ от API: null");
            }

            return answer;
        }
    }
}
