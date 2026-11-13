var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    // По умолчанию ASP.NET сам требует все string-поля и отвечает на английском.
    // Нам нужны свои проверки с понятным русским текстом, поэтому отключаем автоматическое «обязательно».
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Открываешь http://localhost:ПОРТ/ и сразу попадаешь в Swagger
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}

// Тестовые данные (задание 7). Выключить: "SeedTestData": false в appsettings.json
if (app.Configuration.GetValue<bool>("SeedTestData", true))
{
    SeedData.Load();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
