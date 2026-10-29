using System.Security.Claims;
using System.Text.Json.Serialization;
using AsistentePersonal.Data;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Enums travel as text ("Daily", "Weekly"). Numbers are rejected, so an unknown value like 99 never reaches the services.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)));

// Secrets come from the environment, never from a committed file (constitution, principle 5).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing connection string 'Default' (environment variable ConnectionStrings__Default).");
var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Missing 'Jwt:SigningKey' (environment variable Jwt__SigningKey).");

// Throws if the key is shorter than 32 characters.
var sessionTokens = new SessionTokens(signingKey);

// Throws if OWNER_TIME_ZONE is missing or unknown, so a bad configuration stops the app here
// instead of failing on every habits request.
OwnerClock.ResolveZone(builder.Configuration[OwnerClock.TimeZoneVariable]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = sessionTokens.ValidationParameters;
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(sessionTokens);
builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<HabitService>();
builder.Services.AddScoped<IdeaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();

// Sliding session (spec 002, CA-2.5): every response to a signed-in request carries a renewed token,
// valid for 7 days from now. The client replaces the stored token with it.
app.Use(async (context, next) =>
{
    if (int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var ownerId))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Refreshed-Token"] = sessionTokens.Issue(ownerId, DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        });
    }

    await next();
});

app.UseAuthorization();
app.MapControllers();

app.Run();
