using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;
using AsistentePersonal.Data;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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

// The reminders need the Resend key and a shared secret for the scheduled flow (spec 005, RNF-2).
// The secret is checked for its length here, so a weak one stops the app at startup.
var resendApiKey = builder.Configuration["Resend:ApiKey"];
if (string.IsNullOrWhiteSpace(resendApiKey))
{
    throw new InvalidOperationException("Missing 'Resend:ApiKey' (environment variable Resend__ApiKey).");
}

var reminderSecret = new ReminderSecret(builder.Configuration["Reminders:Secret"]
    ?? throw new InvalidOperationException("Missing 'Reminders:Secret' (environment variable Reminders__Secret)."));

// Throws if OWNER_TIME_ZONE is missing or unknown, so a bad configuration stops the app here
// instead of failing on every habits request.
OwnerClock.ResolveZone(builder.Configuration[OwnerClock.TimeZoneVariable]);

// Render terminates HTTPS in front of the app, so the scheme and client address come from the forwarded headers.
// Its proxy addresses are not fixed, so the app accepts them from any address; the app uses them only for
// the HTTPS redirect, never for a security decision.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Only the published frontend may call the API from a browser (spec 005, CA-5.5). Without
// Cors__AllowedOrigin no origin is allowed, which is the safe default.
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (!string.IsNullOrWhiteSpace(allowedOrigin))
    {
        policy.WithOrigins(allowedOrigin);
    }

    policy.WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Authorization", "Content-Type", "X-Reminder-Secret")
        .WithExposedHeaders("X-Refreshed-Token");
}));

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
builder.Services.AddSingleton(reminderSecret);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReminderService>();
builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resendApiKey);
    client.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors();
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
