using Opilochka.Data.Answers;
using Opilochka.Data.Lessons;
using Opilochka.Data.Users;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Opilochka.Data.Tasks;

/// <summary>
/// Задача
/// </summary>
public class Task : DataItem
{
    [JsonIgnore] public Lesson ?Lesson;

    [Required] public long LessonId { get; set; }

    [JsonIgnore] public IEnumerable<ResultCheck> ?ResultChecks;

    [JsonIgnore] public IEnumerable<Answer> ?Answers;

    [JsonIgnore] public User ?User;

    [Required] public long UserId { get; set; }

    [Required] public string Title { get; set; } = string.Empty;

    [Required] public string Description { get; set; } = string.Empty;
    [Required] public string Input { get; set; } = string.Empty;
    [Required] public string Output { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
}