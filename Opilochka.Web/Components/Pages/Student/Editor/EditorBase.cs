using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Opilochka.Web.Security;
using Opilochka.Web.Services.Admin;
using Opilochka.Web.Services.Student;
using Opilochka.Web.Services.Teacher;

namespace Opilochka.Web.Components.Pages.Student.Editor
{
    public class EditorBase : ComponentBase
    {
        [Inject] public NavigationManager? NavigationManager { get; set; }
        [Inject] public ILogger<EditorBase>? Logger { get; set; }
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public UserService UserService { get; set; } = default!;
        [Inject] public CompilerService CompilerService { get; set; } = default!;
        [Inject] public TaskService TaskService { get; set; } = default!;
        [Inject] public JWTAuthenticationStateProvider StateProvider { get; set; } = default!;

        public Data.Tasks.Task? OneTask { get; set; } = new();
        public string? AnswerCompiler {  get; set; } = "Здесь будет отображаться вывод программы";
        public string EditorContent { get; set; } = string.Empty;
        public string SelectedLanguage { get; set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;

        [Parameter] public long TaskId { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            var uri = NavigationManager?.ToAbsoluteUri(NavigationManager.Uri);
            var segments = uri.Segments;

            if (segments.Length > 1 && long.TryParse(segments[^1], out long taskId))
            {
                TaskId = taskId;
                await LoadTaskAsync(TaskId);
            }

            if (firstRender)
            {
                await InitializeEditor();
            }
        }

        //private async Task LoadDataAsync()
        //{
        //    try
        //    {
                
        //    }
        //    catch (Exception ex)
        //    {
        //        LogAndDisplayError(ex, "Ошибка при инициализации");
        //    }
        //}

        private async Task InitializeEditor()
        {
            try
            {
                await JS.InvokeVoidAsync("initializeMonaco", SelectedLanguage);
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка инициализации редактора");
            }
        }
        public async Task CompileCode()
        {
            try
            {
                await UpdateEditorContent();

                var compilerResponse = await CompilerService.GetAnswerCompilerAsync(EditorContent, SelectedLanguage);
                Logger?.LogInformation("Ответ" + compilerResponse.error);
                Logger?.LogInformation("Ошибка" + compilerResponse.output);

                if (compilerResponse != null)
                {
                    if (!string.IsNullOrEmpty(compilerResponse.error))
                    {
                        AnswerCompiler = $"Ошибка: {compilerResponse.error}";
                    }
                    else
                    {
                        AnswerCompiler = compilerResponse.output ?? "Вывод отсутствует";
                    }
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка получения значения редактора");
            }
        }



        public async Task OnLanguageChange(ChangeEventArgs e)
        {
            if (e.Value is string selectedLanguage && !string.IsNullOrWhiteSpace(selectedLanguage))
            {
                SelectedLanguage = selectedLanguage;
                await InitializeEditor().ConfigureAwait(false);
            }
        }
        public async Task UpdateEditorContent()
        {
            try
            {
                EditorContent = await JS.InvokeAsync<string>("getEditorValue");
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка получения значения редактора");
            }
        }

        private void LogAndDisplayError(Exception ex, string message)
        {
            Logger?.LogError(ex, message);
            Message = message;
        }

        private async Task LoadTaskAsync(long id)
        {
            OneTask = await TaskService.GetTaskAsync(id);
        }
    }
}
