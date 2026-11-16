using System.Reflection;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// HTTP-клієнт для звернень до UserService, адреса з appsettings.json
var userServiceUrl = builder.Configuration["Services:UserServiceUrl"]
    ?? throw new InvalidOperationException("Services:UserServiceUrl is not set");
builder.Services.AddHttpClient("UserService", c => c.BaseAddress = new Uri(userServiceUrl));

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OrderService", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService v1"));

app.MapControllers();

app.Run();
