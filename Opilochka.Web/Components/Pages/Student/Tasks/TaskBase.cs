using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Opilochka.Core.ViewModels;
using Opilochka.Data.Answers;
using Opilochka.Data.Lessons;
using Opilochka.Web.Services.Admin;
using Opilochka.Web.Services.Student;
using Opilochka.Web.Services.Teacher;
using System.Security.Claims;

namespace Opilochka.Web.Components.Pages.Student.Tasks
{
    public class TaskBase : ComponentBase
    {
        [Inject] public NavigationManager? NavigationManager { get; set; }
        [Inject] public ILogger<TaskBase>? Logger { get; set; }
        [Inject] public LessonService LessonService { get; set; } = default!;
        [Inject] public TaskService TaskService { get; set; } = default!;
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public AnswerService AnswerService { get; set; } = default!;
        [Inject] public ResultCheckService ResultCheckService { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        public List<Lesson> Lessons { get; set; } = [];
        public List<Data.Tasks.Task>? ActiveTasks { get; set; } = [];
        public List<Data.Tasks.Task>? CheckTasks { get; set; } = [];
        public List<Data.Tasks.Task>? NoCheckTasks { get; set; } = [];
        public List<Answer> Answers { get; set; } = [];
        public List<Data.Tasks.ResultCheck> ResultChecks { get; set; } = [];
        public LessonRequest Lesson { get; set; } = new();

        private ClaimsPrincipal User = new();
        public Data.Tasks.Task? OneTask { get; set; } = new();
        public string Message { get; private set; } = string.Empty;
        [Parameter] public long TaskId { get; set; }
        [Parameter] public long UserId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadUserInfoAsync();
            await LoadDataAsync();
        }
        private async Task LoadDataAsync()
        {
            try
            {
                Lessons = await LessonService.GetLessonsAsync() ?? [];
                Answers = await AnswerService.GetAnswersAsync() ?? [];
                ResultChecks = await ResultCheckService.GetResultChecksAsync() ?? [];
                await TasksAsync();

                if (TaskId != 0)
                {
                    await LoadTaskAsync(TaskId);
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при инициализации списка уроков");
            }
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
        private void LogAndDisplayError(Exception ex, string message)
        {
            Logger?.LogError(ex, message);
            Message = message;
        }

        private async Task TasksAsync()
        {
            var tasks = await TaskService.GetTasksAsync() ?? [];

            Logger?.LogInformation("Получено задач: {tasks.Count}", tasks.Count);

            var userTasks = tasks.Where(task => task.UserId == UserId).ToList();
            Logger?.LogInformation("Задачи для пользователя {UserId}: {userTasks.Count}", UserId, userTasks.Count);

            foreach (var task in userTasks)
            {
                bool hasAnswer = Answers.Any(answer => answer.TaskId == task.Id);
                bool hasResultCheck = ResultChecks.Any(resultCheck => resultCheck.TaskId == task.Id);

                Logger?.LogInformation("Обрабатываем задачу {task.Id} (Ответы: {hasAnswer}, Проверки: {hasResultCheck})", task.Id, hasAnswer, hasResultCheck);

                if (hasAnswer)
                {
                    if (hasResultCheck)
                    {
                        CheckTasks?.Add(task);
                        Logger?.LogInformation("Задача {task.Id} добавлена в CheckTasks", task.Id);
                    }
                    else
                    {
                        NoCheckTasks?.Add(task);
                        Logger?.LogInformation("Задача {task.Id} добавлена в NoCheckTasks", task.Id);
                    }
                }
                else
                {
                    ActiveTasks?.Add(task);
                    Logger?.LogInformation("Задача {task.Id} добавлена в ActiveTasks", task.Id);
                    Logger?.LogInformation("ActiveTasks.Count", ActiveTasks.Count);
                }
            }
            Logger?.LogInformation("Обработка задач завершена.");
        }

        public void NavigationEditorTask(long id)
        {
            TaskId = id;
            NavigationManager?.NavigateTo($"/active-task/editor{TaskId}");
        }
        private async Task LoadTaskAsync(long id)
        {
            OneTask = await TaskService.GetTaskAsync(id);
        }

        public async Task SaveAnswer(string code, string compiler)
        {
            try
            {
                var createdTask = await AnswerService.CreateAnswerAsync(code, compiler, UserId, TaskId);
                await ResultCheckService.CreateResultCheckAsync(OneTask, createdTask);
                if (createdTask != null)
                {
                    Logger?.LogInformation("Добавлен ответ");
                    NavigationBack();
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при сохранении урока");
            }
        }

        public void NavigationBack() => NavigationManager?.NavigateTo("/active-task");

    }
}
