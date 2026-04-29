using System.Text.Json;
using Digiprise.PMS.API.Hubs;
using Digiprise.PMS.API.Middleware;
using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Application.Services;
using Digiprise.PMS.Domain.Interfaces;
using Digiprise.PMS.Infrastructure.Data;
using Digiprise.PMS.Infrastructure.Repositories;
using Digiprise.PMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── In-Memory Data Store (Singleton — shared across all requests) ───────
builder.Services.AddSingleton<InMemoryDataStore>();

// ── Domain Repositories ────────────────────────────────────────────────
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<ISprintRepository, SprintRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// ── Application Services ───────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IIssueService, IssueService>();
builder.Services.AddScoped<ISprintService, SprintService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ── Infrastructure Services ────────────────────────────────────────────
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// ── Authentication (custom JWT handler, no external packages) ─────────
builder.Services.AddAuthentication("PmsJwt")
    .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>("PmsJwt", null);
builder.Services.AddAuthorization();

// ── Controllers ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ── SignalR ─────────────────────────────────────────────────────────────
builder.Services.AddSignalR(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());

// ── CORS ────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("PmsCors", p =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins != null && origins.Length > 0)
            p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        else
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// ── Health Checks ───────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Seed Demo Data ──────────────────────────────────────────────────────
var store = app.Services.GetRequiredService<InMemoryDataStore>();
DataSeeder.Seed(store, app.Logger);

// ── Middleware Pipeline ─────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Inline API documentation page at /docs
    app.MapGet("/docs", () => Results.Content(BuildApiDocsHtml(), "text/html"));
    app.MapGet("/", () => Results.Redirect("/docs"));
}

app.UseHttpsRedirection();
app.UseCors("PmsCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();  // serves index.html at /
app.UseStaticFiles();   // serves wwwroot (full SPA UI)

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.MapControllers();
app.MapHub<PmsHub>("/hubs/pms");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Logger.LogInformation("🚀 Digiprise PMS API running in {Env} mode", app.Environment.EnvironmentName);
app.Logger.LogInformation("📖 API Docs:  http://localhost:5000/docs");
app.Logger.LogInformation("❤️  Health:    http://localhost:5000/health/ready");
app.Logger.LogInformation("🔑 Login:     POST /api/v1/auth/login");

app.Run();

// ── Inline API Explorer HTML ────────────────────────────────────────────
static string BuildApiDocsHtml() => """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Digiprise PMS API</title>
  <style>
    *{box-sizing:border-box;margin:0;padding:0}
    body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:#0f172a;color:#e2e8f0}
    .header{background:linear-gradient(135deg,#1e3a5f,#2563eb);padding:2rem;text-align:center}
    .header h1{font-size:2rem;font-weight:700;color:#fff}
    .header p{color:#93c5fd;margin-top:.5rem}
    .container{max-width:1100px;margin:2rem auto;padding:0 1rem}
    .card{background:#1e293b;border-radius:12px;padding:1.5rem;margin-bottom:1.5rem;border:1px solid #334155}
    .card h2{font-size:1.1rem;font-weight:600;color:#60a5fa;margin-bottom:1rem}
    .endpoint{display:flex;align-items:center;gap:.75rem;padding:.6rem 0;border-bottom:1px solid #1e293b}
    .endpoint:last-child{border:none}
    .method{font-size:.7rem;font-weight:700;padding:.25rem .6rem;border-radius:4px;min-width:56px;text-align:center}
    .get{background:#065f46;color:#6ee7b7}.post{background:#1e3a5f;color:#93c5fd}
    .put{background:#451a03;color:#fbbf24}.delete{background:#4c0519;color:#fca5a5}
    .patch{background:#312e81;color:#a5b4fc}
    .path{font-family:'SF Mono',monospace;font-size:.85rem;color:#94a3b8}
    .desc{font-size:.8rem;color:#64748b;margin-left:auto}
    .creds{background:#0f2027;border:1px solid #1d4ed8;border-radius:8px;padding:1rem;margin:1rem 0}
    .creds code{color:#60a5fa;font-family:monospace;font-size:.9rem;display:block;margin:.25rem 0}
    .grid{display:grid;grid-template-columns:1fr 1fr;gap:1rem}
    @media(max-width:640px){.grid{grid-template-columns:1fr}}
    .badge{background:#1d4ed8;color:#bfdbfe;padding:.2rem .6rem;border-radius:999px;font-size:.7rem;font-weight:600}
  </style>
</head>
<body>
<div class="header">
  <h1>🚀 Digiprise PMS API</h1>
  <p>Jira-like Project Management System &nbsp;|&nbsp; Enterprise Edition v1.0 &nbsp;|&nbsp; ASP.NET Core 8</p>
</div>
<div class="container">
  <div class="creds">
    <strong style="color:#93c5fd">🔑 Demo Credentials</strong>
    <code>Admin: admin@demo.digiprise.io / Admin@123!</code>
    <code>Dev:   dev@demo.digiprise.io   / Dev@123!</code>
    <code>Login: POST /api/v1/auth/login?tenantId=1</code>
  </div>
  <div class="grid">
    <div class="card">
      <h2>🔐 Authentication</h2>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/auth/login</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/auth/register</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/auth/refresh</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/auth/logout</span></div>
    </div>
    <div class="card">
      <h2>📁 Projects</h2>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/projects</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/projects</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/projects/{id}</span></div>
      <div class="endpoint"><span class="method put">PUT</span><span class="path">/api/v1/projects/{id}</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/projects/{id}/archive</span></div>
      <div class="endpoint"><span class="method delete">DEL</span><span class="path">/api/v1/projects/{id}</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/projects/{id}/members</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/projects/{id}/sprints</span></div>
    </div>
    <div class="card">
      <h2>🎫 Issues (IQL Search Supported)</h2>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/issues/{id}</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/issues</span></div>
      <div class="endpoint"><span class="method put">PUT</span><span class="path">/api/v1/issues/{id}</span></div>
      <div class="endpoint"><span class="method patch">PATCH</span><span class="path">/api/v1/issues/{id}</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/issues/{id}/transitions</span></div>
      <div class="endpoint"><span class="method delete">DEL</span><span class="path">/api/v1/issues/{id}</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/issues/search</span><span class="desc badge">IQL</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/issues/bulk</span><span class="desc badge">50 max</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/issues/project/{id}</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/issues/project/{id}/backlog</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/issues/{id}/comments</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/issues/{id}/comments</span></div>
    </div>
    <div class="card">
      <h2>🏃 Sprints</h2>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/sprints/{id}</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/sprints/active/{boardId}</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/sprints/{id}/start</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/sprints/{id}/close</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/sprints/{id}/burndown</span></div>
    </div>
    <div class="card">
      <h2>📊 Dashboard</h2>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/dashboard/summary</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/dashboard/assigned-to-me</span></div>
    </div>
    <div class="card">
      <h2>🔔 Notifications</h2>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/api/v1/notifications/unread</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/notifications/{id}/read</span></div>
      <div class="endpoint"><span class="method post">POST</span><span class="path">/api/v1/notifications/read-all</span></div>
    </div>
    <div class="card">
      <h2>⚡ Real-time (SignalR)</h2>
      <div class="endpoint"><span class="method get">WSS</span><span class="path">/hubs/pms</span></div>
      <div class="endpoint" style="flex-wrap:wrap;gap:.25rem">
        <span class="badge">IssueStatusChanged</span>
        <span class="badge">IssueAssigned</span>
        <span class="badge">CommentAdded</span>
        <span class="badge">SprintStarted</span>
        <span class="badge">NotificationReceived</span>
      </div>
    </div>
    <div class="card">
      <h2>❤️ Health</h2>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/health/live</span></div>
      <div class="endpoint"><span class="method get">GET</span><span class="path">/health/ready</span></div>
    </div>
  </div>
  <div class="card" style="background:#0f2a1a;border-color:#166534">
    <h2 style="color:#4ade80">🏗️ Architecture</h2>
    <p style="color:#86efac;font-size:.9rem;line-height:1.7">
      <strong>5-Layer Clean Architecture</strong> — Domain → Contracts → Application → Infrastructure → API<br>
      <strong>Design Patterns:</strong> Repository, CQRS (ready), Domain Events, Strategy, Factory<br>
      <strong>Auth:</strong> Custom JWT (HS256) · 15-min access token · 7-day refresh token · Role-based<br>
      <strong>Real-time:</strong> ASP.NET Core SignalR hub (board, project & user groups)<br>
      <strong>Storage:</strong> In-memory ConcurrentDictionary (swap to EF Core + SQL Server for production)<br>
      <strong>Features:</strong> IQL search · Bulk ops (50 max) · Sprint lifecycle · Burndown · Notifications
    </p>
  </div>
</div>
</body>
</html>
""";
