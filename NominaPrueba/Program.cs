using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics;
using NominaPrueba;

var builder = WebApplication.CreateBuilder(args);

// Hosting variables and command-line URLs take precedence over the local default.
if (string.IsNullOrEmpty(builder.Configuration["urls"]) &&
    string.IsNullOrEmpty(builder.Configuration["HTTP_PORTS"]) &&
    string.IsNullOrEmpty(builder.Configuration["HTTPS_PORTS"]))
{
    var port = builder.Configuration["PORT"] ?? "5000";
    if (!int.TryParse(port, out var number) || number is < 1 or > 65535)
        throw new InvalidOperationException("PORT debe ser un puerto entre 1 y 65535.");
    builder.WebHost.UseUrls($"http://0.0.0.0:{number}");
}

var apiKey = builder.Configuration["Security:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 32)
    throw new InvalidOperationException("Configura Security__ApiKey con al menos 32 caracteres.");
var expectedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
var cipher = new NominaCipher(builder.Configuration["Encryption:Key"]);
builder.Services.AddSingleton(cipher);
builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 10 * 1024 * 1024);

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = error is BadHttpRequestException requestError
        ? requestError.StatusCode : 500;
    await context.Response.WriteAsJsonAsync(new { error = context.Response.StatusCode == 413
        ? "La solicitud supera el limite de 10 MiB."
        : "Error al procesar la solicitud." });
}));
app.Use(async (context, next) =>
{
    if (context.Request.Path != "/health")
    {
        var supplied = context.Request.Headers["X-Api-Key"];
        if (supplied.Count != 1 || !CryptographicOperations.FixedTimeEquals(
                expectedKeyHash, SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0] ?? ""))))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key invalida o ausente." });
            return;
        }
    }
    context.Response.Headers["Cache-Control"] = "no-store";
    if (context.Request.ContentLength > 10 * 1024 * 1024)
    {
        context.Response.StatusCode = 413;
        await context.Response.WriteAsJsonAsync(new { error = "La solicitud supera el limite de 10 MiB." });
        return;
    }
    await next();
});
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();
app.Run();
