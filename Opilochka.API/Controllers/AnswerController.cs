using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opilochka.Data.Answers;

namespace Opilochka.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AnswerController(OpilochkaDbContext context, ILogger<AnswerController> logger) : Controller
    {
        private readonly ILogger<AnswerController> _logger = logger;
        private readonly OpilochkaDbContext _context = context;

        [Authorize]
        [HttpGet(Name = "GetAnswers")]
        public async Task<List<Answer>> Get()
        {
            _logger.LogInformation($"Вызван метод GetAnswers без указания идентификатора");
            var answers = await _context.Answers.Where(b => !b.IsDeleted).ToListAsync();
            _logger.LogInformation($"Метод GetAnswers возвращает ответы студентов");
            return answers;
        }

        [Authorize]
        [HttpPost(Name = "CreateAnswer")]
        public async Task<IActionResult> Post([FromBody] Answer answer)
        {
            if (answer == null)
            {
                _logger.LogWarning("Получен пустой объект ответа");
                return BadRequest("Ответ не может быть пустым");
            }

            try
            {
                _context.Answers.Add(answer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ответ добавлен: {Id}", answer.Id);

                return CreatedAtAction(nameof(Post), new { id = answer.Id }, answer);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка при обновлении базы данных");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка подключения" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении ответа");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка при создании пользователя" });
            }
        }

        [Authorize]
        [HttpPut("{id}", Name = "UpdateAnswer")]
        public async Task<IActionResult> Put(long id, [FromBody] Answer answer)
        {
            if (answer == null)
            {
                _logger.LogWarning("Получен пустой объект ответа для обновления");
                return BadRequest("Ответ не может быть пустым");
            }

            try
            {
                var answerId = await _context.Answers
                    .Where(b => b.Id == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.TextAnswer, b => answer.TextAnswer)
                        .SetProperty(b => b.AnswerStatus, b => answer.AnswerStatus));

                if (answerId > 0)
                {
                    _logger.LogInformation("Ответ с идентификатором {id} успешно обновлён", id);
                    return Ok(answerId);
                }
                else
                {
                    _logger.LogWarning("Ответ с идентификатором {id} не найден для обновления", id);
                    return NotFound("Ответ не найден");
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка при обновлении ответа с идентификатором {id}", id);
                return StatusCode(500, "Произошла ошибка при обновлении базы данных");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении ответа с идентификатором {id}", id);
                return StatusCode(500, "Произошла ошибка при обновлении ответа");
            }
        }

        [Authorize]
        [HttpDelete("{id}", Name = "DeleteAnswer")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Вызван метод DeleteAnswer с идентификатором: {id}", id);

            var updatedRows = await _context.Answers
                .Where(b => b.Id == id && !b.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, b => true));

            if (updatedRows > 0)
            {
                _logger.LogInformation("Ответ с идентификатором {id} успешно удален", id);
                return Ok(new { Message = $"Ответ с идентификатором {id} успешно удален" });
            }

            _logger.LogWarning("Ответ с идентификатором {id} не найден или уже удален", id);
            return NotFound(new { Error = $"Ответ с идентификатором {id} не найден" });
        }
    }
}
