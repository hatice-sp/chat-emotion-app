using ChatEmotionBackend.Data;
using ChatEmotionBackend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace ChatEmotionBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public MessageController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }

        // POST: api/Message
        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] Message message)
        {
            if (string.IsNullOrEmpty(message.Text) || message.UserId == 0)
                return BadRequest("Geçersiz mesaj veya kullanıcı.");

            message.Sentiment = "unknown";

            try
            {
                // Python microservice'e POST at
                var response = await _httpClient.PostAsJsonAsync(
                    "http://127.0.0.1:5000/analyze",  // Python servis URL
                    new { text = message.Text }
                );

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("sentiment", out var sentimentProp))
                        message.Sentiment = sentimentProp.GetString();
                }
            }
            catch
            {
                // Hata olursa sentiment "unknown" kalır
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        // GET: api/Message
        [HttpGet]
        public IActionResult GetMessages()
        {
            var messages = _context.Messages
                .Select(m => new {
                    m.Id,
                    m.UserId,
                    m.Text,
                    m.Sentiment
                }).ToList();

            return Ok(messages);
        }
        [HttpDelete("reset")]
        public async Task<IActionResult> ResetDatabase()
        {
          var messages = _context.Messages.ToList();
         _context.Messages.RemoveRange(messages);
          await _context.SaveChangesAsync();
          return Ok("Veritabanı temizlendi.");
        }

    }
}
