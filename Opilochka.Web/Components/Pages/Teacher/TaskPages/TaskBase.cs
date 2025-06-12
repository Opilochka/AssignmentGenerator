using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Opilochka.Data.Lessons;
using Opilochka.Data.StudyGroups;
using Opilochka.Data.Tasks;
using Opilochka.Data.Users;
using Opilochka.Web.Services.Admin;
using Opilochka.Web.Services.Teacher;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace Opilochka.Web.Components.Pages.Teacher.TaskPages
{
    public class TaskBase : ComponentBase
    {
        [Inject] public NavigationManager? NavigationManager { get; set; }
        [Inject] public ILogger<TaskBase>? Logger { get; set; }
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public TaskService TaskService { get; set; } = default!;
        [Inject] public GroupService GroupService { get; set; } = default!;
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public LessonService LessonService { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        public List<TaskResponse> TasksPreview { get; set; } = [];
        public List<StudyGroup> Groups { get; private set; } = [];
        public List<User> Users { get; set; } = [];
        public List<User> UsersTask { get; set; } = [];
        public List<Lesson> Lessons { get; set; } = [];

        public HashSet<TaskResponse> updatedTasks = [];
        public TaskRequest TaskRequest { get; set; } = new();
        public StudyGroup StudyGroup { get; set; } = new();
        public Lesson Lesson { get; set; } = new();

        private ClaimsPrincipal User = new();
        public string Message { get; private set; } = string.Empty;
        public bool ActivateLoader;

        [Parameter] public long UserId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
            await LoadUserInfoAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Groups = await GroupService.GetGroupsAsync() ?? [];
                Users = await UserService.GetUsersAsync() ?? [];
                Lessons = await LessonService.GetLessonsAsync() ?? [];
                
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при инициализации списка пользователей");
            }
        }

        public async Task TasksPreviewGenerate()
        {
            ActivateLoader = true;
            TasksPreview.Clear();
            if (StudyGroup != null && TaskRequest != null)
            {

                Logger?.LogInformation("Начинаем генерацию предварительного просмотра задач");
                foreach (var user in Users ?? Enumerable.Empty<User>())
                {
                    Console.WriteLine(StudyGroup.Id);
                    if (user.GroupId == StudyGroup.Id)
                    {
                        TaskResponse? response;
                        do
                        {
                            Logger?.LogInformation("Запрос предварительного просмотра задачи");
                            response = await TaskService.TaskPreviewAsync(TaskRequest);
                        }
                        while (response == null);

                        UsersTask.Add(user);
                        Logger?.LogInformation("Получен ответ на предварительный просмотр задачи");
                        TasksPreview.Add(response);
                    }
                }

                Logger?.LogInformation("Генерация предварительного просмотра задач завершена");
            }
            else
            {
                Logger?.LogWarning("StudyGroup или TaskRequest равны null");
            }
            ActivateLoader = false;
        }

        public async Task TaskUpPreviewGenerate(TaskResponse taskResponse)
        {
            Logger?.LogInformation("Начинаем генерацию предварительного просмотра задачи");

            TaskResponse? taskResponseNew;
            do
            {
                Logger?.LogInformation("Запрос предварительного просмотра задачи");
                taskResponseNew = await TaskService.TaskPreviewAsync(TaskRequest);
            }
            while (taskResponseNew == null);

            Logger?.LogInformation("Получен ответ на предварительный просмотр задачи");
            Console.WriteLine(TasksPreview);

            TasksPreview[TasksPreview.IndexOf(taskResponse)] = taskResponseNew;
        }

        public async Task SaveTaskAsync(TaskResponse request, long lessonId, long userId)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Запрос не может быть пустым");
            }

            try
            {
                await TaskService.SaveTaskAsync(request, lessonId, userId);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Ошибка при сохранении задачи: {Request}", request);
                throw;
            }

            updatedTasks.Add(request);
        }

        public bool IsTaskUpdated(TaskResponse task)
        {
            return updatedTasks.Contains(task);
        }
        private void LogAndDisplayError(Exception ex, string message)
        {
            Logger?.LogError(ex, message);
            Message = message;
        }

        private async Task LoadUserInfoAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            User = authState.User;

            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst("Id")?.Value;
                UserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : 0;
            }
        }
    }
}
