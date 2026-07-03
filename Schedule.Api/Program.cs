var builder = WebApplication.CreateBuilder(args);

// 1. Додаємо сервіси Swashbuckle Swagger для генерації документації
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Додаємо підтримку контролерів (знадобиться для твоїх майбутніх API-ендпоінтів)
builder.Services.AddControllers();

var app = builder.Build();

// 2. Налаштовуємо HTTP-пайплайн
// Включаємо сторінку Swagger у браузері лише для локальної розробки (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Налаштовуємо Swagger UI на корінь сайту (не обов'язково, але зручно)
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Schedule API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
