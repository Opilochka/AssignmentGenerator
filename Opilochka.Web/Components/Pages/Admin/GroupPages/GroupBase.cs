using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Opilochka.Data.StudyGroups;
using Opilochka.Web.Components.Pages.Admin.User;
using Opilochka.Web.Services.Admin;

namespace Opilochka.Web.Components.Pages.Admin.GroupPages
{
    public class GroupBase : ComponentBase
    {
        [Inject] public NavigationManager? NavigationManager { get; set; }
        [Inject] public ILogger<GroupBase>? Logger { get; set; }
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public GroupService GroupService { get; set; } = default!;

        public List<StudyGroup> Groups { get; private set; } = new();
        public List<StudyGroup> FilteredGroups { get; private set; } = new();
        public StudyGroup? GroupUpdate { get; set; } = new();
        public StudyGroup Group { get; set; } = new();
        public string Message { get; private set; } = string.Empty;
        public string Title { get; private set; } = "Группы";
        [Parameter] public long GroupId { get; set; }
        [Parameter] public long GroupDelId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Groups = await GroupService.GetGroupsAsync() ?? [];
                FilteredGroups = new List<StudyGroup>(Groups);

                if (GroupId != 0)
                {
                    await LoadGroupAsync(GroupId);
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при инициализации списка групп");
            }
        }
        protected Task UpdateListGroups(string searchTerm)
        {
            FilteredGroups.Clear();
            FilteredGroups.AddRange(string.IsNullOrEmpty(searchTerm)
                ? Groups
                : Groups.Where(group =>
                    group.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

            StateHasChanged();
            return Task.CompletedTask;
        }
        public async Task SaveGroup()
        {
            try
            {
                var createdGroup = await GroupService.CreateGroupAsync(Group);
                Logger?.LogInformation($"Добавлена группа: {Group?.Name}");

                if (createdGroup != null)
                {
                    NavigationBack();
                }
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex, "Ошибка при сохранении группы");
            }
        }
        public async Task UpdateGroup()
        {
            try
            {
                if (GroupUpdate == null)
                {
                    Logger?.LogWarning("Попытка обновления группы, но GroupUpdate равен null");
                    Message = "Не удалось обновить данные, группа не найден";
                    return;
                }

                await GroupService.UpdateGroupAsync(GroupId, GroupUpdate);
                NavigationBack();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Ошибка при обновлении группы");
                Message = "Произошла ошибка при обновлении данных. Пожалуйста, попробуйте снова";
            }
        }
        public async Task IdDel(long id)
        {
            GroupDelId = id;
            await JS.InvokeAsync<string>("openModal");
        }
        public async Task HandleDeleteGroup(long id)
        {
            GroupId = id;
            try
            {
                await GroupService.DeleteGroupAsync(GroupId);
                NavigationBack();
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Ошибка при удалении группы: {ex.Message}");
            }
        }
        public void NavigationBack() => NavigationManager?.NavigateTo("/groups");

        public void NavigationCreate() => NavigationManager?.NavigateTo("groups/create");

        public async void NavigationUpdate(long id)
        {
            GroupId = id;
            GroupUpdate = await GroupService.GetGroupAsync(id);
            NavigationManager?.NavigateTo("groups/update/" + id);
        }
        public async Task LoadGroupAsync(long id)
        {
            Logger?.LogInformation($"Загрузка группы с ID: {id}");
            GroupUpdate = await GroupService.GetGroupAsync(id);

            if (GroupUpdate == null)
            {
                Logger?.LogWarning($"Группа с ID {id} не найденf");
            }
        }

        private void LogAndDisplayError(Exception ex, string message)
        {
            Logger?.LogError(ex, message);
            Message = message;
        }
    }
}
