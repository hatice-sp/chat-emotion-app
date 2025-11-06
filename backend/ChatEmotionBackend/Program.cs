using ChatEmotionBackend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// ✅ SQLite veritabanı (dosya proje dizininde saklanacak)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=chat.db"));

// ✅ Python servisine istek atmak için
builder.Services.AddHttpClient();

// ✅ CORS ayarı (Render + Vercel izinli)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",                // yerel geliştirme
                "https://chat-emotion-frontend.vercel.app" // Vercel'deki domain (senin domaininle değiştir)
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger sadece development’ta çalışsın
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS yönlendirmesini devre dışı bırakıyoruz (Render HTTP kullanır)
app.UseHttpsRedirection();

// CORS aktif
app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

// ✅ Render’ın PORT değişkenine göre dinleme
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

app.Run();
