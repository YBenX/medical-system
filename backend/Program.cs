using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置数据库
builder.Services.AddDbContext<MedicalDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 配置服务
builder.Services.AddHttpClient<IAIService, AIService>();
builder.Services.AddSingleton<IWorkflowService, WorkflowService>();

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 添加控制器
builder.Services.AddControllers();

// 添加Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 配置HTTP请求管道
// 启用Swagger（开发和生产环境都可用）
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowVueApp");

app.UseAuthorization();

app.MapControllers();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
