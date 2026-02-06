using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加 CORS 策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// 在所有环境中启用 Swagger（包括生产环境）
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll"); // 启用 CORS

// 不使用 HTTPS 重定向，仅支持 HTTP
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();