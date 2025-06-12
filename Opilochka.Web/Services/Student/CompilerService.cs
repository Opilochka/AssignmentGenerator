using Opilochka.Data.Answers;
using Opilochka.Web.Services.Auth;
using System.Text.Json;

namespace Opilochka.Web.Services.Student
{
    public class CompilerService(ILogger<CompilerService> logger, APIService service)
    {
        private readonly ILogger<CompilerService> _logger = logger;
        private readonly APIService _service = service;

        public async Task<CompilerResponse?> GetAnswerCompilerAsync(string code, string language)
        {
            _logger.LogInformation("Вызван метод GetAnswerCompilerAsync");

            var apiResponse = await _service.PostAsync("Compiler", new CompileRequest { Code = code, Language = language });

            if (!apiResponse.IsSuccessStatusCode)
            {
                var responseBody = await apiResponse.Content.ReadAsStringAsync();
                _logger.LogError("Ошибка API: {ResponseBody}", responseBody);
                return new CompilerResponse { output = null, error = responseBody };
            }

            var responseBodyString = await apiResponse.Content.ReadAsStringAsync();

            var response = JsonSerializer.Deserialize<CompilerResponse>(responseBodyString);

            _logger.LogInformation("Answer" + responseBodyString);

            if (response == null)
            {
                _logger.LogError("Не удалось десериализовать ответ API: {ResponseBody}", responseBodyString);
            }

            return response;
        }
    }
}
