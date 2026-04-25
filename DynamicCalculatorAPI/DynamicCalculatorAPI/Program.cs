using DynamicCalculatorAPI.DBContext;
using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Repository;
using DynamicCalculatorAPI.Services;
using DynamicPaymentCalc.Services;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<PaymentContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IMethodEngine, DynamicExpressoService>();
builder.Services.AddScoped<IMethodEngine, SQLDynamicService>();
builder.Services.AddScoped<IMethodEngine, RoslynFormulaService>();

builder.Services.AddScoped<IEngineService, EngineOrchestrator>();
builder.Services.AddScoped<ICalculateService, CalculateService>();

builder.Services.AddScoped<IJobService, JobService>(); 
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger"; // זה מבטיח שהכתובת תהיה /swagger
    });
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
