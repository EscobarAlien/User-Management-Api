using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5213);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddAuthentication();
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<LoggingMiddleware>();

app.MapGet("/", () => "Hello, and Welcome to my Middleware of UserManagementApi");

app.MapGet("/about/", () => "This will show you the about section of the application");

app.MapGet("/contact/info/", () => "This will be available soon");

app.MapGet("/testimonial/", () => "This is a temporary usage to test other http functions on it");

app.MapPost("/post/testimonial/", () => 
{
    return "This is a temporary usage to test other http functionality on it";
});
app.MapPut("/put/testimonial/", () => {
    return "This is a put test for the Middleware";
    });
app.MapDelete("/delete/testimonial/", () => {
    return "This is a delete test for the Middleware";
});

app.Use(async (context, next) => 
{
    await next();

    if(context.Response.StatusCode >= 400)
    {
        Console.WriteLine($"Security Event: {context.Request.Path} - Status Code: {context.Response.StatusCode}");
    }
});

app.Use(async (context, next) => 
{
    if (context.Request.Query["secure"] != "true")
    {
        context.Response.StatusCode =400;
        await context.Response.WriteAsync("Simulated HTTPS Required");
        return;
    }
    await next();
});

app.Use(static async (context, next) =>
{
    var input = context.Request.Query["input"];
    if (!IsValidInput(input))
    {
        if  (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid Input");
        }
        return;
    }
    await next();
});

static bool IsValidInput(String input)
{
    return string.IsNullOrEmpty(input) || (input.All(char.IsLetterOrDigit) && !input.Contains("<script>"));
}

app.Use(async (context, next) => 
{
    if (context.Request.Path == "/unauthorized")
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode =401;
            await context.Response.WriteAsync("Unauthorized Access");
        }
        return;
    }
    await next();
});

app.Use(async (context, next) => 
{
    var isAuthenticated = context.Request.Query["isAuthenticated"] == "true";
    if (!isAuthenticated)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode =403;
            await context.Response.WriteAsync("Access Denied");
        }
        return;
    }
    context.Response.Cookies.Append("SecureCookie", "SecureData", new CookieOptions
    {
        HttpOnly =true,
        Secure =true
    });

    await next();
});

app.Use(async (context, next) => 
{
    await Task.Delay(100);
    if (!context.Response.HasStarted)
    {
        await context.Response.WriteAsync("Processed Asynchonously");
    }
    await next();
});

app.Run(async (context) => 
{
    if (!context.Response.HasStarted)
    {
        await context.Response.WriteAsync("Final Response from Application");
    }
});

app.Run();

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception
            Debug.WriteLine($"Unhandled exception: {ex.Message}");

            // Return a JSON response
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;
            var errorResponse = new { error = "Internal server error." };
            var errorJson = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(errorJson);
        }
    }
}

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log the request path
        Debug.WriteLine($"Request Path: {context.Request.Path}");

        // Call the next middleware in the pipeline
        await _next(context);

        // Log the response status code
        Debug.WriteLine($"Response Status Code: {context.Response.StatusCode}");
    }
}