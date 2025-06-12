using System.ComponentModel.DataAnnotations;

namespace Opilochka.Data.Tasks
{
    public class TaskRequest
    {
        [Required] public string Title { get; set; } = string.Empty;

        [Required] public decimal Difficult { get; set; }

        [Required] public decimal CountTests { get; set; } 

    }
}
