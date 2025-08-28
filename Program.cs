using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Nuevo endpoint para consultar clima real desde OpenWeatherMap
app.MapGet("/weather/{city}", async (string city, IHttpClientFactory httpClientFactory) =>
{
    // API key de OpenWeatherMap (puede ser configurada en appsettings.json)
    var apiKey = "demo"; // Esta es una key de demostración, debería ser reemplazada
    var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric&lang=es";
    
    try
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenWeatherResponse>(content);
            
            if (weatherData != null)
            {
                return Results.Ok(new
                {
                    City = weatherData.Name,
                    Country = weatherData.Sys?.Country,
                    Temperature = weatherData.Main?.Temp,
                    FeelsLike = weatherData.Main?.FeelsLike,
                    Humidity = weatherData.Main?.Humidity,
                    Description = weatherData.Weather?.FirstOrDefault()?.Description,
                    WindSpeed = weatherData.Wind?.Speed
                });
            }
        }
        
        return Results.Problem("No se pudo obtener la información del clima");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al consultar la API del clima: {ex.Message}");
    }
})
.WithName("GetRealWeather")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Clases para deserializar la respuesta de OpenWeatherMap
public class OpenWeatherResponse
{
    public string? Name { get; set; }
    public MainData? Main { get; set; }
    public WeatherData[]? Weather { get; set; }
    public WindData? Wind { get; set; }
    public SysData? Sys { get; set; }
}

public class MainData
{
    public double Temp { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
}

public class WeatherData
{
    public string? Description { get; set; }
}

public class WindData
{
    public double Speed { get; set; }
}

public class SysData
{
    public string? Country { get; set; }
}
