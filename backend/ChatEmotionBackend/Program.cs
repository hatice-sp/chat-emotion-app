using ChatEmotionBackend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(); // Controller'ları çalıştırmak için

// SQLite DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=chat.db"));

// HTTP Client Factory (Python servisine çağrı yapabilmek için)
builder.Services.AddHttpClient();

// CORS ayarı (React frontend izinli)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // React frontend URL
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS middleware’i UseAuthorization’dan önce ekleyin
app.UseCors("AllowReactApp");

app.UseAuthorization();

// Controller'ları etkinleştir
app.MapControllers();

app.Run();
