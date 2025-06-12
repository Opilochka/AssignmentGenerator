using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.VisualBasic;
using Opilochka.Data.StudyGroups;
using Opilochka.Data.Users;
using Opilochka.Web.Security;
using Opilochka.Web.Services.Admin;

namespace Opilochka.Web.Components.Pages.Admin.User
{
    public class UserBase : ComponentBase
    {

        [Inject] public NavigationManager? NavigationManager { get; set; }
        [Inject] public ILogger<UserBase>? Logger { get; set; }
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public GroupService GroupService { get; set; } = default!;
        [Inject] public JWTAuthenticationStateProvider stateProvider { get; set; } = default!;

        public List<Data.Users.User> Users { get; private set; } = new();
        public List<StudyGroup> Groups { get; private set; } = new();
        public List<Data.Users.User> FilteredUsers { get; private set; } = new();
        public UserRequest User { get; set; } = new();
        public Data.Users.User? UserUpdate { get; set; } = new();
        public string Message { get; private set; } = string.Empty;
        public string Title { get; private set; } = "Пользователи";

        [Parameter] public long UserId { get; set; }
        [Parameter] public long UserDelId { get; set; }


        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var state = stateProvider.GetAuthenticationStateAsync();
                var usersResult = await UserService.GetUsersAsync();
                Users = usersResult ?? [];

                var groupsResult = await GroupService.GetGroupsAsync();
                Groups = groupsResult ?? [];

                FilteredUsers = new List<Data.Users.User>(Users);

                if (UserId != 0)
                {
                    await LoadUserAsync(UserId);
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при инициализации списка пользователей");
            }
        }

        protected Task UpdateListUsers(string searchTerm)
        {
            FilteredUsers.Clear();
            FilteredUsers.AddRange(string.IsNullOrEmpty(searchTerm)
                ? Users
                : Users.Where(user =>
                    user.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    user.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

            StateHasChanged();
            return Task.CompletedTask;
        }

        public async Task SaveUser()
        {
            try
            {
                var createdUser = await UserService.CreateUserAsync(User);
                
                if (createdUser != null)
                {
                    Logger?.LogInformation($"Добавлен пользователь: {User?.Email}");
                    NavigationBack();
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при сохранении пользователя");
            }
        }

        public async Task UpdateUser()
        {
            try
            {
                if (UserUpdate == null)
                {
                    Logger?.LogWarning("Попытка обновления пользователя, но UserUpdate равен null");
                    Message = "Не удалось обновить данные, пользователь не найден";
                    return;
                }

                await UserService.UpdateUserAsync(UserId, UserUpdate);
                NavigationBack();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Ошибка при обновлении пользователя");
                Message = "Произошла ошибка при обновлении данных. Пожалуйста, попробуйте снова";
            }
        }

        public async Task IdDel(long id)
        {
            UserDelId = id;
            await JS.InvokeAsync<string>("openModal");
        }
        public async Task HandleDeleteUser()
        {
            try
            {
                await UserService.DeleteUserAsync(UserDelId);
                NavigationBack();
                await JS.InvokeAsync<string>("closeModal");
                await JS.InvokeAsync<string>("RefreshUser");
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Ошибка при удалении пользователя: {ex.Message}");
            }
        }

        public void NavigationBack() => NavigationManager?.NavigateTo("/users");

        public void NavigationCreate() => NavigationManager?.NavigateTo("users/create");

        public async Task NavigationUpdate(long id)
        {
            UserId = id;
            UserUpdate = await UserService.GetUserAsync(id);
            NavigationManager?.NavigateTo("users/update/" + id);
        }

        public async Task LoadUserAsync(long id)
        {
            Logger?.LogInformation($"Загрузка пользователя с ID: {id}");
            UserUpdate = await UserService.GetUserAsync(id);

            if (UserUpdate == null)
            {
                Logger?.LogWarning($"Пользователь с ID {id} не найден.");
            }
        }

        private void LogAndDisplayError(Exception ex, string message)
        {
            Logger?.LogError(ex, message);
            Message = message;
        }
    }
}
