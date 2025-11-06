using ChatEmotionBackend.Data;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ChatEmotionBackend.Controllers
{
    [ApiController]
    [Route("analyze")]
    public class AnalyzeController : ControllerBase
    {
        private readonly HttpClient _client;
        public AnalyzeController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MessageDto message)
        {
            // Hugging Face Spaces URL
            string flaskUrl = "https://hatice10-chat-emotion-ai.hf.space/analyze";

            // POST isteği gönder
            var response = await _client.PostAsJsonAsync(flaskUrl, message);
            var content = await response.Content.ReadAsStringAsync();

            return Content(content, "application/json");
        }
    }

    public class MessageDto
    {
        public int UserId { get; set; }
        public string Text { get; set; }
    }
}

