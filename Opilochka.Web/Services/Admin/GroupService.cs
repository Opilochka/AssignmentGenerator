using Opilochka.Data.StudyGroups;
using Opilochka.Data.Users;
using Opilochka.Web.Services.Auth;
using System.Net.Http;

namespace Opilochka.Web.Services.Admin
{
    public class GroupService(
        APIService aPIService,
        ILogger<GroupService> logger,
        HttpRequestHandler httpRequestHandler)
    {
        private readonly APIService _service = aPIService;
        private readonly ILogger<GroupService> _logger = logger;
        private readonly HttpRequestHandler _httpRequestHandler = httpRequestHandler;

        public async Task<List<StudyGroup>?> GetGroupsAsync()
        {
            _logger.LogInformation("Вызван метод GetGroups");

            var groups = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync("Group");
                return await _httpRequestHandler.HandleResponseAsync<List<StudyGroup>>(response);
            });

            if (groups == null || groups.Count == 0)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но список групп пуст");
            }
            else
            {
                _logger.LogInformation("Получено {GroupCount} групп", groups.Count);
            }

            return groups;
        }

        public async Task<StudyGroup?> GetGroupAsync(long id)
        {
            _logger.LogInformation("Вызван метод GetGroup для группы с ID {GroupId}", id);

            var group = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync($"Group/{id}");
                return await _httpRequestHandler.HandleResponseAsync<StudyGroup>(response);
            });

            if (group == null)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но группа с ID {GroupId} не найдена", id);
            }
            else
            {
                _logger.LogInformation("Получена группа: {@Group}", group);
            }

            return group;
        }

        public async Task<StudyGroup?> CreateGroupAsync(StudyGroup request)
        {
            _logger.LogInformation("Вызван метод CreateGroup с данными: {@Request}", request);

            var group = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PostAsync("Group", request);
                _logger.LogInformation("Запрос на создание группы отправлен. Статус ответа: {StatusCode}", response.StatusCode);

                var groupResponse = await _httpRequestHandler.HandleResponseAsync<StudyGroup>(response);
                return groupResponse;
            });

            if (group != null)
            {
                _logger.LogInformation("Группа успешно создана: {@Group}", group);
            }
            else
            {
                _logger.LogWarning("Создание группы завершилось неудачно. Ответ от API: null");
            }

            return group;
        }

        public async Task<StudyGroup?> UpdateGroupAsync(long id, StudyGroup request)
        {
            if (request == null)
            {
                _logger.LogError("Ошибка: Запрос не может быть null для обновления группы с идентификатором: {GroupId}", id);
                throw new ArgumentNullException(nameof(request), "Запрос не может быть null");
            }

            _logger.LogInformation("Вызван метод UpdateGroupAsync для группы с идентификатором: {GroupId} с данными: {@Request}", id, request);

            return await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PutAsync($"Group/{id}", request);
                _logger.LogInformation("Запрос на обновление группы с идентификатором: {GroupId} отправлен. Статус ответа: {StatusCode}", id, response.StatusCode);

                var group = await _httpRequestHandler.HandleResponseAsync<StudyGroup>(response);

                if (group != null)
                {
                    _logger.LogInformation("Группа с идентификатором: {GroupId} успешно обновлена: {@Group}", id, group);
                }
                else
                {
                    _logger.LogWarning("Обновление группы с идентификатором: {GroupId} завершилось неудачно. Ответ от API: null.", id);
                }

                return group;
            });
        }

        public async Task DeleteGroupAsync(long id)
        {
            _logger.LogInformation("Вызван метод DeleteGroupAsync для группы с идентификатором: {GroupId}", id);

            await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.DeleteAsync($"Group/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Ошибка при удалении группы с идентификатором {id}: {response.StatusCode} - {errorMessage}");
                }

                _logger.LogInformation("Группа с идентификатором {id} успешно удалена", id);

                return Task.CompletedTask;
            });
        }
    }
}
