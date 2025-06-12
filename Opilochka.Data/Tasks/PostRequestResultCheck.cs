using Opilochka.Data.Answers;


namespace Opilochka.Data.Tasks
{
    public class PostRequestResultCheck
    {
        public Task Task { get; set; } = new();
        public Answer Answer { get; set; } = new();
    }
}
