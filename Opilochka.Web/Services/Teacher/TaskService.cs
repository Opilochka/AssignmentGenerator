using Opilochka.Data.Lessons;
using Opilochka.Data.Tasks;
using Opilochka.Web.Services.Auth;


namespace Opilochka.Web.Services.Teacher
{
    public class TaskService(ILogger<TaskService> logger, HttpRequestHandler httpRequestHandler, APIService aPIService)
    {
        private readonly ILogger<TaskService> _logger = logger;
        private readonly HttpRequestHandler _httpRequestHandler = httpRequestHandler;
        private readonly APIService _service = aPIService;

        public async Task<List<Data.Tasks.Task>?> GetTasksAsync()
        {
            _logger.LogInformation("Вызван метод GetTasks");

            var tasks = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync("Task");
                return await _httpRequestHandler.HandleResponseAsync<List<Data.Tasks.Task>>(response);
            });

            if (tasks == null || !tasks.Any())
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но список заданий пуст");
            }
            else
            {
                _logger.LogInformation("Получено {TaskCount} заданий", tasks.Count);
            }

            return tasks;
        }
        public async Task<TaskResponse> TaskPreviewAsync(TaskRequest request)
        {
            _logger.LogInformation("Начало выполнения TaskPreviewAsync. Запрос: {@Request}", request);

            try
            {
                _logger.LogInformation("Выполнение HTTP-запроса к сервису Task/TaskPreview");

                var taskPreview = await _httpRequestHandler.ExecuteAsync(async () =>
                {
                    var response = await _service.PostAsync("Task/TaskPreview", request);
                    _logger.LogInformation("Получен ответ от сервиса: {StatusCode}", response.StatusCode);

                    var taskResponse = await _httpRequestHandler.HandleResponseAsync<TaskResponse>(response);
                    _logger.LogInformation("Обработан ответ, получен TaskResponse: {@TaskResponse}", taskResponse);

                    return taskResponse;
                });

                return taskPreview ?? new TaskResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при выполнении TaskPreviewAsync");
                throw;
            }
        }
        public async Task<TaskResponse?> SaveTaskAsync(TaskResponse task, long lessonID, long userID)
        {
            var taskToSave = new Data.Tasks.Task()
            {
                LessonId = lessonID,
                UserId = userID,
                Title = task.Title,
                Description = task.Description,
                Input = task.Input,
                Output = task.Output,
            };

            try
            {
                var response = await _service.PostAsync("Task", taskToSave);
                _logger.LogInformation("Получен ответ от сервиса: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var taskResponse = await _httpRequestHandler.HandleResponseAsync<TaskResponse>(response);
                    _logger.LogInformation("Обработан ответ, получен TaskResponse: {@TaskResponse}", taskResponse);
                    return taskResponse;
                }
                else
                {
                    _logger.LogWarning("Ошибка при сохранении задачи: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при сохранении задачи");
                throw;
            }
        }
        public async Task<Data.Tasks.Task?> GetTaskAsync(long id)
        {
            _logger.LogInformation("Вызван метод GetTask для задачи с ID {TaskId}", id);

            var task = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync($"Task/{id}");
                return await _httpRequestHandler.HandleResponseAsync<Data.Tasks.Task>(response);
            });

            if (task == null)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но задача с ID {TaskId} не найдена", id);
            }
            else
            {
                _logger.LogInformation("Получена задача: {@Task}", task);
            }

            return task;
        }

    }
}
