using Opilochka.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Opilochka.Core.ViewModels
{
    public class LessonRequest
    {
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public long UserId { get; set; }
        [Required] public LessonIcon Icon { get; set; }
    }
}
