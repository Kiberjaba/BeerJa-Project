using BeejaServer.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключаем базу данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Обязательно для Сваги
builder.Services.AddSwaggerGen();           // Обязательно для Сваги

var app = builder.Build();

app.UseDefaultFiles(); //Ищет индекс штмл в рут
app.UseStaticFiles(); //отдает файлы в рут

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); //подрубает свагу
    app.UseSwaggerUI(); //включает табло сваги
}

app.UseHttpsRedirection();
app.MapControllers(); // Это нужно, чтобы контроллеры заработали

app.Run();