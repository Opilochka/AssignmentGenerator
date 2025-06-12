using System;
namespace Opilochka.Core.ViewModels
{
    public class CreateTask
    {
        public long LessonId { get; set; }

        public long UserId { get; set; }

        public string Title { get; set; } = string.Empty;

    }
}
