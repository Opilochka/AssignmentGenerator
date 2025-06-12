using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Opilochka.Core.ViewModels;
using Opilochka.Data.Answers;
using Opilochka.Data.Enums;
using Opilochka.Data.Lessons;
using Opilochka.Data.StudyGroups;
using Opilochka.Data.Users;
using Opilochka.Web.Services.Admin;
using Opilochka.Web.Services.Student;
using Opilochka.Web.Services.Teacher;
using System.Security.Claims;

namespace Opilochka.Web.Components.Pages.Teacher.LessonPages
{
    public class LessonBase : ComponentBase
    {
        [Inject] public NavigationManager? NavigationManager { get; set; }
        [Inject] public ILogger<LessonBase>? Logger { get; set; }
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public LessonService LessonService { get; set; } = default!;
        [Inject] public TaskService TaskService { get; set; } = default!;
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public AnswerService AnswerService { get; set; } = default!;
        [Inject] public GroupService GroupService { get; set; } = default!;
        [Inject] public ResultCheckService ResultCheckService { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        public List<Lesson> Lessons { get; private set; } = [];
        public List<User> Users { get; private set; } = [];
        public List<Data.Tasks.Task> ActiveTasks { get; private set; } = [];
        public List<Data.Tasks.Task> CheckTasks { get; private set; } = [];
        public List<Data.Tasks.Task> NoCheckTasks { get; private set; } = [];
        public List<Data.Tasks.ResultCheck> ResultChecks { get; set; } = [];
        public List<Answer> Answers { get; set; } = [];
        public List<StudyGroup> Groups { get; set; } = [];
        public List<Lesson> FilteredLessons { get; private set; } = [];
        public LessonRequest Lesson { get; set; } = new();
        public Data.Tasks.Task? OneTask { get; set; } = new();
        public Data.Tasks.ResultCheck? ResultCheck { get; set; } = new();
        public Answer? Answer { get; set; } = new();
        public Lesson? LessonOutput { get; set; } = new();
        public Lesson? LessonUpdate { get; set; } = new();

        private ClaimsPrincipal User = new();
        public string Message { get; private set; } = string.Empty;
        public string Title { get; private set; } = "Уроки";

        [Parameter] public long LessonId { get; set; }
        [Parameter] public long TaskId { get; set; }
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
                Lessons = await LessonService.GetLessonsAsync() ?? [];
                Users = await UserService.GetUsersAsync() ?? [];
                Answers = await AnswerService.GetAnswersAsync() ?? [];
                Groups = await GroupService.GetGroupsAsync() ?? [];
                ResultChecks = await ResultCheckService.GetResultChecksAsync() ?? [];
                FilteredLessons = new List<Lesson>(Lessons);
                await TasksAsync();

                if (LessonId != 0)
                {
                    await LoadLessonAsync(LessonId);
                }

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
        protected Task UpdateListLessons(string searchTerm)
        {
            FilteredLessons.Clear();
            FilteredLessons.AddRange(string.IsNullOrEmpty(searchTerm)
                ? Lessons
                : Lessons.Where(lesson =>
                    lesson.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                    
            StateHasChanged();
            return Task.CompletedTask;
        }
        public async Task SaveLesson()
        {
            try
            {
                Lesson.UserId = UserId;
                var createdLesson = await LessonService.CreateLessonAsync(Lesson);

                if (createdLesson != null)
                {
                    Logger?.LogInformation("Добавлен урок: {Title}", Lesson.Title);
                    NavigationBack();
                    await JS.InvokeAsync<string>("RefreshUser");
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при сохранении урока");
            }
        }
        public async Task UpdateLesson()
        {
            try
            {
                if (LessonOutput == null)
                {
                    Logger?.LogWarning("Попытка обновления урока, но LessonUpdate равен null");
                    Message = "Не удалось обновить данные, урок не найден";
                    return;
                }

                await LessonService.UpdateLessonAsync(UserId, LessonOutput);
                NavigationBack();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Ошибка при обновлении урока");
                Message = "Произошла ошибка при обновлении данных. Пожалуйста, попробуйте снова";
            }
        }
        public async Task UpdateResult()
        {
            try
            {
                if (ResultCheck == null)
                {
                    Logger?.LogWarning("Попытка обновления оценки, но ResultCheck равен null");
                    Message = "Не удалось обновить данные, оценка не найден";
                    return;
                }

                await ResultCheckService.UpdateResultCheckAsync(ResultCheck.Id, ResultCheck);
                NavigationBack();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Ошибка при обновлении оценки");
                Message = "Произошла ошибка при обновлении данных. Пожалуйста, попробуйте снова";
            }
        }
        public async Task HandleDeleteLesson()
        {
            try
            {
                await LessonService.DeleteLessonAsync(LessonId);
                NavigationBack();
            }
            catch (Exception ex)
            {
                Logger?.LogError("Ошибка при удалении урока: {Message}", ex.Message);
            }
        }
        public void NavigationBack() => NavigationManager?.NavigateTo("/lessons");
        public void NavigationLesson(long id)
        {
            LessonId = id;
            NavigationManager?.NavigateTo("/lessons/lesson" + LessonId);
        }
        public void NavigationTask(long id)
        {
            TaskId = id;
            NavigationManager?.NavigateTo($"/lessons/lesson{LessonId}/task{TaskId}");
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
        private async Task LoadLessonAsync(long id)
        {
            LessonOutput = await LessonService.GetLessonAsync(id);
        }
        private async Task LoadTaskAsync(long id)
        {
            OneTask = await TaskService.GetTaskAsync(id);
            await AnswerAndResult();
        }
        private void LogAndDisplayError(Exception ex, string message)
        {
            Logger?.LogError(ex, message);
            Message = message;
        }
        public void SetLessonIcon(string id)
        {
            if (int.TryParse(id, out int parsedId) && parsedId >= 1 && parsedId <= 8)
            {
                Lesson.Icon = (LessonIcon)parsedId;
                Logger?.LogInformation("Установлена иконка: {Icon}", Lesson.Icon);
            }
        }
        private async Task TasksAsync()
        {
            var tasks = await TaskService.GetTasksAsync() ?? new List<Data.Tasks.Task>();

            Logger?.LogInformation("Получено задач: {tasks.Count}", tasks.Count);

            var userTasks = tasks.Where(task => task.LessonId == LessonId).ToList();
            Logger?.LogInformation("Задачи для урока {LessonId}: {userTasks.Count}", UserId, userTasks.Count);

            foreach (var task in userTasks)
            {
                bool hasAnswer = Answers.Any(answer => answer.TaskId == task.Id);
                bool hasResultCheck = ResultChecks.Any(resultCheck => resultCheck.TaskId == task.Id);

                Logger?.LogInformation("Обрабатываем задачу {task.Id} (Ответы: {hasAnswer}, Проверки: {hasResultCheck})", task.Id, hasAnswer, hasResultCheck);

                if (hasAnswer)
                {
                    if (hasResultCheck)
                    {
                        CheckTasks.Add(task);
                        Logger?.LogInformation("Задача {task.Id} добавлена в CheckTasks", task.Id);
                    }
                    else
                    {
                        NoCheckTasks.Add(task);
                        Logger?.LogInformation("Задача {task.Id} добавлена в NoCheckTasks", task.Id);
                    }
                }
                else
                {
                    ActiveTasks.Add(task);
                    Logger?.LogInformation("Задача {task.Id} добавлена в ActiveTasks", task.Id);
                }
            }
            Logger?.LogInformation("Обработка задач завершена.");
        }

        private async Task AnswerAndResult()
        {
            foreach (var answer in Answers)
            {
                if(OneTask.Id == answer.TaskId)
                {
                    Answer = answer;
                    break;
                }
            }

            foreach (var resultCheck in ResultChecks)
            {
                if (resultCheck.TaskId == OneTask.Id)
                {
                    ResultCheck = resultCheck;
                }
            }

        }
    }
}
