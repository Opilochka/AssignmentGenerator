using Opilochka.Core.ViewModels;
using Opilochka.Data.Lessons;
using Opilochka.Web.Services.Auth;

namespace Opilochka.Web.Services.Teacher
{
    public class LessonService
    {
        private readonly APIService _service;
        private readonly ILogger<LessonService> _logger;
        private readonly HttpRequestHandler _httpRequestHandler;

        public LessonService(ILogger<LessonService> logger, HttpRequestHandler httpRequestHandler, APIService aPIService)
        {
            _logger = logger;
            _httpRequestHandler = httpRequestHandler;
            _service = aPIService;
        }

        public async Task<List<Lesson>?> GetLessonsAsync()
        {
            _logger.LogInformation("Вызван метод GetLessons");

            var lessons = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync("Lesson");
                return await _httpRequestHandler.HandleResponseAsync<List<Lesson>>(response);
            });

            if (lessons == null || !lessons.Any())
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но список уроков пуст");
            }
            else
            {
                _logger.LogInformation("Получено {LessonCount} уроков", lessons.Count);
            }

            return lessons;
        }
        public async Task<Lesson?> GetLessonAsync(long id)
        {
            _logger.LogInformation("Вызван метод GetLesson для урока с ID {LessonId}", id);

            var lesson = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync($"Lesson/{id}");
                return await _httpRequestHandler.HandleResponseAsync<Lesson>(response);
            });

            if (lesson == null)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но урок с ID {LesssonId} не найден", id);
            }
            else
            {
                _logger.LogInformation("Получен урок: {@Lesson}", lesson);
            }

            return lesson;
        }
        public async Task<Lesson?> CreateLessonAsync(LessonRequest request)
        {
            _logger.LogInformation("Вызван метод CreateLesson с данными: {@Request}", request);

            var lesson = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PostAsync("Lesson", request);
                _logger.LogInformation("Запрос на создание урока отправлен. Статус ответа: {StatusCode}", response.StatusCode);

                var lessonResponse = await _httpRequestHandler.HandleResponseAsync<Lesson>(response);
                return lessonResponse;
            });

            if (lesson != null)
            {
                _logger.LogInformation("Урок успешно создан: {@Lesson}", lesson);
            }
            else
            {
                _logger.LogWarning("Создание урока завершилось неудачно. Ответ от API: null");
            }

            return lesson;
        }
        public async Task<Lesson?> UpdateLessonAsync(long id, Lesson request)
        {
            if (request == null)
            {
                _logger.LogError("Ошибка: Запрос не может быть null для обновления урока с идентификатором: {LessonId}", id);
                throw new ArgumentNullException(nameof(request), "Запрос не может быть null");
            }

            _logger.LogInformation("Вызван метод UpdateLessonAsync для урока с идентификатором: {LessonId} с данными: {@Request}", id, request);

            return await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PutAsync($"Lesson/{id}", request);
                _logger.LogInformation("Запрос на обновление урока с идентификатором: {LessonId} отправлен. Статус ответа: {StatusCode}", id, response.StatusCode);

                var lesson = await _httpRequestHandler.HandleResponseAsync<Lesson>(response, id);

                if (lesson != null)
                {
                    _logger.LogInformation("Урок с идентификатором: {LessonId} успешно обновлён: {@Lesson}", id, lesson);
                }
                else
                {
                    _logger.LogWarning("Обновление пользователя с идентификатором: {LessonId} завершилось неудачно. Ответ от API: null.", id);
                }

                return lesson;
            });
        }
        public async Task DeleteLessonAsync(long id)
        {
            _logger.LogInformation("Вызван метод DeleteLessonAsync для урока с идентификатором: {LessonId}", id);

            await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.DeleteAsync($"Lesson/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Ошибка при удалении урока с идентификатором {id}: {response.StatusCode} - {errorMessage}");
                }

                _logger.LogInformation($"Урок с идентификатором {id} успешно удален");

                return Task.CompletedTask;
            });
        }
    }
}
