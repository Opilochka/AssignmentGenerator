using Opilochka.Data.Enums;
using Opilochka.Data.Users;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Task = Opilochka.Data.Tasks.Task;

namespace Opilochka.Data.Lessons;

public class Lesson : DataItem
{
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public long UserId { get; set; }
    [Required] public LessonIcon Icon { get; set; }

    [JsonIgnore] public IEnumerable<Task> Tasks { get; private set; } = Enumerable.Empty<Task>();
    [JsonIgnore] public User? User { get; private set; }

}