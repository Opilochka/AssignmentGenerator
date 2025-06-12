using Opilochka.Core.Enums;
using Opilochka.Data.Users;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Task = Opilochka.Data.Tasks.Task;

namespace Opilochka.Data.Answers;


public class Answer : DataItem
{
    [Required] public long TaskId { get; set; }
    [Required] public long UserId { get; set; }
    [Required] public string TextAnswer { get; set; } = string.Empty;
    [Required] public string TextCompiler {  get; set; } = string.Empty;
    [JsonIgnore] public User? User {  get; set; }
    [JsonIgnore] public Task? Task { get; set; }

    public DateTime DateTime { get; set; }
    public AnswerStatus AnswerStatus { get; set; } = AnswerStatus.NotChecked;
}   