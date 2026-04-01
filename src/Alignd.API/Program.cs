using System.Text;
using Alignd.API.Hubs;
using Alignd.API.Middleware;
using Alignd.Application.Participants.ClaimAdmin;
using Alignd.Application.Participants.Disconnect;
using Alignd.Application.Participants.JoinRoom;
using Alignd.Application.Participants.SetWatcher;
using Alignd.Application.Rooms.CreateRoom;
using Alignd.Application.Tasks.UploadTasks;
using Alignd.Application.Voting.CastVote;
using Alignd.Application.Voting.EndRound;
using Alignd.Application.Voting.NextTask;
using Alignd.Application.Voting.StartRound;
using Alignd.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DB, repos, profanity, auth, SignalR notifier) ─────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Application handlers ──────────────────────────────────────────────────────
builder.Services.AddScoped<CreateRoomHandler>();
builder.Services.AddScoped<JoinRoomHandler>();
builder.Services.AddScoped<StartRoundHandler>();
builder.Services.AddScoped<CastVoteHandler>();
builder.Services.AddScoped<EndRoundHandler>();
builder.Services.AddScoped<NextTaskHandler>();
builder.Services.AddScoped<ClaimAdminHandler>();
builder.Services.AddScoped<DisconnectHandler>();
builder.Services.AddScoped<SetWatcherHandler>();
builder.Services.AddScoped<UploadTasksHandler>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ClockSkew                = TimeSpan.Zero
        };

        // SignalR sends JWT via query string
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path  = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors    = builder.Environment.IsDevelopment();
    opts.ClientTimeoutInterval   = TimeSpan.FromSeconds(60);
    opts.KeepAliveInterval       = TimeSpan.FromSeconds(15);
});

// ── CORS — allow Angular dev server ──────────────────────────────────────────
builder.Services.AddCors(opts => opts.AddPolicy("alignd-cors", policy =>
    policy
        .WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Alignd API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();   // must be first

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("alignd-cors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<VotingHub>("/hubs/voting");

app.Run();
