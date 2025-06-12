using Opilochka.Core.Enums;
using Opilochka.Data.Answers;
using Opilochka.Data.StudyGroups;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Task = Opilochka.Data.Tasks.Task;

namespace Opilochka.Data.Users;

/// <summary>
/// Пользователь
/// </summary>
public class User : DataItem
{
    public User()
    {
        Tasks = new List<Task>();
        Answers = new List<Answer>();
    }

    [JsonIgnore] public StudyGroup? Group;

    public long? GroupId { get; set; }

    [JsonIgnore] public IEnumerable<Task> Tasks;

    [JsonIgnore] public IEnumerable<Answer> Answers;

    [Required] public string SecondName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    [Required] public string FirstName { get; set; } = string.Empty;

    [Required]
    [EmailAddress] public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [Required] public Role Role { get; set; } = Role.Admin;
}