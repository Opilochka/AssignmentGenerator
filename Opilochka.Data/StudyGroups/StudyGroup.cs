using Opilochka.Data.Users;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Opilochka.Data.StudyGroups;

/// <summary>
/// Учебная группа
/// </summary>
public class StudyGroup : DataItem
{
    [Required] public string Name { get; set; } = string.Empty;

    [JsonIgnore] public IEnumerable<User> Users { get; set; } = new List<User>();
}