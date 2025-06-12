using System.ComponentModel.DataAnnotations;

namespace Opilochka.Data.StudyGroups
{
    public class GroupRequest
    {
        [Required] public string Name { get; set; } = string.Empty;
    }
}
