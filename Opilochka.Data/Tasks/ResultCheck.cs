using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Opilochka.Data.Tasks;

public class ResultCheck : DataItem
{
    [Required] public long TaskId { get; set; }
    [Required] public bool Result { get; set; }
    [Required] public decimal Scores { get; set; }
    [Required] public string Comment { get; set; } = string.Empty;

    [JsonIgnore] public Task? Task;
    public DateTime AssessmentDate { get; set; }
}