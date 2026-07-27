using System.Text;
using BeejaServer.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Сервисы
builder.Services.AddSingleton<JwtService>();

// Настраиваем аутентификацию через JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// Подключаем базу данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Обязательно для Сваги
builder.Services.AddSwaggerGen();           // Обязательно для Сваги

var app = builder.Build();

// Настройка HTTP-пайплайна (ПОРЯДОК ВАЖЕН!)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // подрубает свагу
    app.UseSwaggerUI(); // включает табло сваги
}

app.UseDefaultFiles();  // Ищет index.html в wwwroot
app.UseStaticFiles();   // Отдает статические файлы

app.UseHttpsRedirection();

// Аутентификация и Авторизация строго перед MapControllers!
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers(); // Маппинг контроллеров

app.Run();м