using Opilochka.Data.Users;
using Opilochka.Web.Services.Auth;

namespace Opilochka.Web.Services.Admin
{
    public class UserService(
        ILogger<UserService> logger,
        HttpRequestHandler httpRequestHandler,
        APIService aPIService)
    {
        private readonly ILogger<UserService> _logger = logger;
        private readonly HttpRequestHandler _httpRequestHandler = httpRequestHandler;
        private readonly APIService _service = aPIService;

        public async Task<List<User>?> GetUsersAsync()
        {
            _logger.LogInformation("Вызван метод GetUsers");

            var users = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync("User");
                return await _httpRequestHandler.HandleResponseAsync<List<User>>(response);
            });

            if (users == null || users.Count == 0)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но список пользователей пуст");
            }
            else
            {
                _logger.LogInformation("Получено {UserCount} пользователей", users.Count);
            }

            return users;
        }

        public async Task<User?> GetUserAsync(long id)
        {
            _logger.LogInformation("Вызван метод GetUser для пользователя с ID {UserId}", id);

            var user = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.GetAsync($"User/{id}");
                return await _httpRequestHandler.HandleResponseAsync<User>(response);
            });

            if (user == null)
            {
                _logger.LogWarning("Запрос к API выполнен успешно, но пользователь с ID {UserId} не найден", id);
            }
            else
            {
                _logger.LogInformation("Получен пользователь: {@User}", user);
            }

            return user;
        }

        public async Task<User?> CreateUserAsync(UserRequest request)
        {
            _logger.LogInformation("Вызван метод CreateUser с данными: {@Request}", request);

            var user = await _httpRequestHandler.ExecuteAsync(async () =>
            {
                //var response = await _httpClient.PostAsJsonAsync("User", request);
                var response = await _service.PostAsync("User", request);
                _logger.LogInformation("Запрос на создание пользователя отправлен. Статус ответа: {StatusCode}", response.StatusCode);

                var userResponse = await _httpRequestHandler.HandleResponseAsync<User>(response);
                return userResponse;
            });

            if (user != null)
            {
                _logger.LogInformation("Пользователь успешно создан: {@User}", user);
            }
            else
            {
                _logger.LogWarning("Создание пользователя завершилось неудачно. Ответ от API: null");
            }

            return user;
        }

        public async Task<User?> UpdateUserAsync(long id, User request)
        {
            if (request == null)
            {
                _logger.LogError("Ошибка: Запрос не может быть null для обновления пользователя с идентификатором: {UserId}", id);
                throw new ArgumentNullException(nameof(request), "Запрос не может быть null");
            }

            _logger.LogInformation("Вызван метод UpdateUserAsync для пользователя с идентификатором: {UserId} с данными: {@Request}", id, request);

            return await _httpRequestHandler.ExecuteAsync(async () =>
            {
                var response = await _service.PutAsync($"User/{id}", request);
                _logger.LogInformation("Запрос на обновление пользователя с идентификатором: {UserId} отправлен. Статус ответа: {StatusCode}", id, response.StatusCode);

                var user = await _httpRequestHandler.HandleResponseAsync<User>(response, id);

                if (user != null)
                {
                    _logger.LogInformation("Пользователь с идентификатором: {UserId} успешно обновлён: {@User}", id, user);
                }
                else
                {
                    _logger.LogWarning("Обновление пользователя с идентификатором: {UserId} завершилось неудачно. Ответ от API: null.", id);
                }

                return user;
            });
        }

        public async Task DeleteUserAsync(long id)
        {
            _logger.LogInformation("Вызван метод DeleteUserAsync для пользователя с идентификатором: {UserId}", id);

            await _httpRequestHandler.ExecuteAsync(async () =>
            {
                //var response = await _httpClient.DeleteAsync($"User/{id}");
                var response = await _service.DeleteAsync($"User/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Ошибка при удалении пользователя с идентификатором {id}: {response.StatusCode} - {errorMessage}");
                }

                _logger.LogInformation("Пользователь с идентификатором {id} успешно удален", id);

                return Task.CompletedTask;
            });
        }

    }
}
