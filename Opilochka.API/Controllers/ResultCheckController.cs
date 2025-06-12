using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Opilochka.Core.Auth;
using Opilochka.Core.OpenAI;
using Opilochka.Data.Answers;
using Opilochka.Data.Tasks;

namespace Opilochka.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultCheckController(OpilochkaDbContext context, IOptions<OpenAIOptions> options, ILogger<ResultCheckController> logger, ILogger<TaskController> loggerT) : Controller
    {
        private readonly ILogger<ResultCheckController> _logger = logger;
        private readonly ILogger<TaskController> _loggerT = loggerT;
        private readonly IOptions<OpenAIOptions> _options = options;
        private readonly OpilochkaDbContext _context = context;

        [Authorize]
        [HttpGet(Name = "GetResults")]
        public async Task<List<Data.Tasks.ResultCheck>> Get()
        {
            _logger.LogInformation("Запрос на получение результатов проверок, которые не были удалены");
            var results = await _context.ResultChecks.Where(b => !b.IsDeleted).ToListAsync();
            _logger.LogInformation("Получено {results.Count} результатов проверок", results.Count);
            return results;
        }

        //[Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpPost(Name = "CreateResult")]
        public async Task<IActionResult> Post([FromBody] PostRequestResultCheck postRequestResultCheck)
        {
            _logger.LogInformation("Вызван метод добавления результата проверки");

            Data.Tasks.ResultCheck respons = new();
            OpenAIController openAIController = new(_options, _loggerT);

            string requestCreateResultCheck = $"Ты - преподаватель по программированию. Твоя задача - оценить выполненное задание студента." +
            $" Код студента:  \"{postRequestResultCheck.Answer.TextAnswer}\", ответ компилятора \"{postRequestResultCheck.Answer.TextCompiler}\". Выданное задание: \"{postRequestResultCheck.Task.Description}\", " +
            $"входные данные: \"{postRequestResultCheck.Task.Input}\"" +
            $",выходные параметры: \"{postRequestResultCheck.Task.Output}\"\".Оцени ответ из 10 баллов. Дай комментрай для студента, опиши за что были снижены баллы и что можно улучшить. " +
                $"\r\nВерни ответ в формате JSON:\r\n{{\r\n   \"Scores\": \"оценка\",\r\n " +
                $" \"Comment\": \"комментарий\"";

            try
            {
                var sendMessageResult = await openAIController.SendMessage(requestCreateResultCheck);

                if (sendMessageResult is OkObjectResult okResult)
                {
                    var answerAI = okResult.Value as string;
                    _logger.LogInformation("Ответ от OpenAI: {answer}", answerAI);

                    if (!string.IsNullOrEmpty(answerAI))
                    {
                        try
                        {
                            var cleanedAnswer = answerAI.Replace("json", "").Trim('`', '\n').Trim();
                            _logger.LogInformation("Очищенный ответ от OpenAI: {cleanedAnswer}", cleanedAnswer);

                            var deserializedResponse = JsonConvert.DeserializeObject<Data.Tasks.ResultCheck>(cleanedAnswer);
                            if (deserializedResponse != null)
                            {
                                respons = deserializedResponse;
                                respons.TaskId = postRequestResultCheck.Task.Id;
                                _logger.LogInformation("Получен ответ: {Title}", respons.Comment);
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
                _context.ResultChecks.Add(respons);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Результат проверки успешно добавлен с идентификатором: {result.Id}", respons.Id);
                return CreatedAtAction(nameof(Post), new { id = respons.Id }, respons);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("Не удалось добавить результат проверки, возникло исключение: {ex.Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка подключения к базе данных" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Неизвестная ошибка: {ex.Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка при добавлении результата проверки" });
            }
        }

        [Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpPut("{id}", Name = "UpdateResult")]
        public async Task<IActionResult> Put(long id, [FromBody] Data.Tasks.ResultCheck result)
        {
            _logger.LogInformation("Вызван метод UpdateResult с идентификатором: {id}", id);

            if (result == null || string.IsNullOrWhiteSpace(result.Comment))
            {
                _logger.LogWarning("Не получены корректные данные для обновления комментария с идентификатором: {id}", id);
                return BadRequest(new { Error = "Некорректные данные для обновления комментария" });
            }

            try
            {
                var updatedRows = await _context.ResultChecks
                    .Where(b => b.Id == id && !b.IsDeleted)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(b => b.Comment, result.Comment));

                if (updatedRows > 0)
                {
                    _logger.LogInformation("Комментарий для результата проверки с идентификатором {id} успешно обновлен", id);
                    return Ok(new { Message = "Комментарий успешно обновлен" });
                }

                _logger.LogWarning("Результат проверки с идентификатором {id} не найден или комментарий не изменен", id);
                return NotFound(new { Error = $"Результат проверки не найден или комментарий не изменен" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("Ошибка при обновлении комментария результата проверки с идентификатором {id}: {ex.Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка при обновлении комментария" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Неизвестная ошибка при обновлении комментария результата проверки с идентификатором {id}: {ex.Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка" });
            }
        }

        [Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpDelete("{id}", Name = "DeleteResult")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Вызван метод DeleteResultCheck с идентификатором: {id}", id);

            var updatedRows = await _context.ResultChecks
                .Where(b => b.Id == id && !b.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, b => true));

            if (updatedRows > 0)
            {
                _logger.LogInformation("Результат проверки с идентификатором {id} успешно удален", id);
                return Ok(new { Message = $"Результат проверки с идентификатором {id} успешно удален" });
            }

            _logger.LogWarning("Результат проверки с идентификатором {id} не найден или уже удален", id);
            return NotFound(new { Error = $"Результат проверки с идентификатором {id} не найден" });
        }
    }
}
