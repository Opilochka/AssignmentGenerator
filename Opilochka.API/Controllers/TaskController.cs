using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Opilochka.Core.Auth;
using Opilochka.Core.OpenAI;
using Opilochka.Data.Tasks;
using Task = Opilochka.Data.Tasks.Task;

namespace Opilochka.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController(OpilochkaDbContext context, IOptions<OpenAIOptions> options, ILogger<TaskController> logger) : Controller
    {
        private readonly OpilochkaDbContext _context = context;
        private readonly IOptions<OpenAIOptions> _options = options;
        private readonly ILogger<TaskController> _logger = logger;

        [Authorize]
        [HttpGet(Name = "GetTasks")]
        public async Task<List<Task>> Get()
        {
            _logger.LogInformation($"Вызван метод GetTasks без указания идентификатора");
            var tasks = await _context.Tasks.Where(b => !b.IsDeleted).ToListAsync();
            _logger.LogInformation($"Метод GetTasks возвращает задания");
            return tasks;
        }

        [Authorize]
        [HttpGet("{id}", Name = "GetTask")]
        public async Task<IActionResult> GetTask(long id)
        {
            _logger.LogInformation("Вызван метод GetTask с идентификатором: {Id}", id);
            var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (task == null)
            {
                _logger.LogWarning("Задание с идентификатором {Id} не найдено", id);
                return NotFound(
                    new
                    {
                        Error = $"Задание с идентификатором {id} не найдено"
                    }
                );
            }

            _logger.LogInformation("Задание с идентификатором  {Id}  не найдено", id);
            return Ok(task);
        }

        //[Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [Authorize]
        [HttpPost(Name = "CreateTask")]
        public async Task<IActionResult> Post([FromBody] Task request)
        {
            _logger.LogInformation($"Вызван метод CreateTask");

            try
            {
                Task task = new()
                {
                    LessonId = request.LessonId,
                    UserId = request.UserId,
                    Title = request.Title,
                    Description = request.Description,
                    Input = request.Input,
                    Output = request.Output,
                    CreationDate = DateTime.UtcNow

                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Задание с успешно создано с идентификатором: {Id}", task.Id);
                return CreatedAtAction(nameof(Post), new { id = task.Id }, task);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("Не удалось создать задание, возникло исключение: {Message}. Внутреннее исключение: {InnerException.Message}", ex.Message, ex.InnerException?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка подключения" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Неизвестная ошибка: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка при создании задания" });
            }
        }

        [Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpDelete("{id}", Name = "DeleteTask")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Вызван метод DeleteTask с идентификатором: {Id}", id);

            var updatedRows = await _context.Tasks
                .Where(b => b.Id == id && !b.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, b => true));

            if (updatedRows > 0)
            {
                _logger.LogInformation("Задание с идентификатором {Id} успешно удалено", id);
                return Ok(new { Message = $"Задание с идентификатором {id} успешно удалено" });
            }

            _logger.LogWarning("Задание с идентификатором {Id} не найдено или уже удалено", id);
            return NotFound(new { Error = $"Задание с идентификатором {id} не найдено" });
        }

        [Authorize]
        [HttpPost("TaskPreview")]
        public async Task<TaskResponse> Post([FromBody] TaskRequest request)
        {
            _logger.LogInformation("Я тут");
            TaskResponse respons = new();

            OpenAIController openAIController = new(_options, _logger);

            string requestCreateTask = $"Ты - преподаватель по программированию. Твоя задача -создать уникальное задание" +
                $" на тему:  \"{request.Title}\", сложность задания \"{request.Difficult}/10;\".Генерируй задания для студента " +
                $"университета.Обязательно придумай входные и выходные параметры соответствующие этому заданию." +
                $"\r\nВерни ответ в формате JSON:\r\n{{\r\n   \"Title\": \"название задания\",\r\n " +
                $" \"Description\": \"описание задания для студента с перечисленными входными параметрами и примером выходных\",\r\n " +
                $" \"Input\": \"входные параметры\",\r\n  \"Output\": \"выходные параметры\"\r\n}}";

            try
            {
                var sendMessageResult = await openAIController.SendMessage(requestCreateTask);

                if (sendMessageResult is OkObjectResult okResult)
                {
                    var answer = okResult.Value as string;
                    _logger.LogInformation("Ответ от OpenAI: {answer}", answer);

                    if (!string.IsNullOrEmpty(answer))
                    {
                        try
                        {
                            var cleanedAnswer = answer.Replace("json", "").Trim('`', '\n').Trim();
                            _logger.LogInformation("Очищенный ответ от OpenAI: {cleanedAnswer}", cleanedAnswer);

                            var deserializedResponse = JsonConvert.DeserializeObject<TaskResponse>(cleanedAnswer);
                            if (deserializedResponse != null)
                            {
                                respons = deserializedResponse;
                                _logger.LogInformation("Получен ответ: {Title}", respons.Title);
                            }
                            else
                            {
                                _logger.LogError("Ошибка десериализации ответа от OpenAI. Ответ не соответствует ожидаемому формату");
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError("Ошибка десериализации JSON: {Message}", jsonEx.Message);
                        }
                    }
                    else
                    {
                        _logger.LogError("Ответ от OpenAI пустой");
                    }
                }
                else
                {
                    var statusCode = (sendMessageResult as StatusCodeResult)?.StatusCode;
                    _logger.LogError("Ошибка при отправке сообщения: {statusCode}", statusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Произошла ошибка: {Message}", ex.Message);
            }

            return respons;
        }
    }
}
