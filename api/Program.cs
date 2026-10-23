using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Monrys.Api.Controllers;
using Monrys.Api.Data;
using Monrys.Api.Services;

Console.WriteLine($"[Monrys] === arranque {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===");
Console.Out.Flush();

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------ proveedor
var proveedor = builder.Configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
var cadenaPg = builder.Configuration.GetConnectionString("Monrys")
    ?? "Host=localhost;Port=5432;Database=monrys;Username=postgres;Password=postgres;SearchPath=monrys";
var cadenaSqlite = builder.Configuration.GetConnectionString("MonrysSqlite")
    ?? "Data Source=monrys.db";

// Diagnóstico: datos de la cadena de conexión (sin exponer la contraseña).
string pgHost = "", pgUser = "", pgPassSha = "";
int pgPort = 0, pgPassLen = 0;
try
{
    var csbInfo = new Npgsql.NpgsqlConnectionStringBuilder(cadenaPg);
    pgHost = csbInfo.Host ?? "";
    pgPort = csbInfo.Port;
    pgUser = csbInfo.Username ?? "";
    pgPassLen = (csbInfo.Password ?? "").Length;
    pgPassSha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(csbInfo.Password ?? "")))[..12].ToLowerInvariant();
}
catch (Exception ex)
{
    pgHost = "ERROR: " + ex.Message;
}

// Pre-flight: si PostgreSQL no responde, caemos a SQLite (modo demo).
// En produccion se desactiva con Database:FallbackSqlite=false: es preferible
// que el contenedor falle de forma visible antes que arrancar en modo demo.
var respaldoSqlite = builder.Configuration.GetValue("Database:FallbackSqlite", true);
string? errorBase = null;

// Pre-flight con timeout corto: sirve para diagnosticar el arranque en PaaS.
// El resultado se expone en /api/health para no depender de los logs del contenedor.
if (proveedor == "postgres")
{
    try
    {
        var csb = new Npgsql.NpgsqlConnectionStringBuilder(cadenaPg) { Timeout = 5 };
        using var pg = new Npgsql.NpgsqlConnection(csb.ConnectionString);
        pg.Open();
        pg.Close();
        Console.WriteLine($"[Monrys] Pre-flight OK -> {csb.Host}:{csb.Port}/{csb.Database} (usuario {csb.Username})");
    }
    catch (Exception ex)
    {
        errorBase = $"{ex.GetType().Name}: {ex.Message}";
        Console.WriteLine($"[Monrys] Pre-flight FALLO -> {errorBase}");
        if (respaldoSqlite)
        {
            proveedor = "sqlite";
            Console.WriteLine("[Monrys] Usando SQLite (modo demo).");
        }
    }
}
Console.Out.Flush();

builder.Services.AddDbContext<MonrysDbContext>(opciones =>
{
    if (proveedor == "sqlite")
    {
        opciones.UseSqlite(cadenaSqlite);
    }
    else
    {
        opciones.UseNpgsql(cadenaPg, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "monrys"));
    }
    opciones.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    if (builder.Environment.IsDevelopment())
    {
        opciones.EnableSensitiveDataLogging();
    }
});

// --------------------------------------------------------------------- token
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MONRYS-DEV-KEY-DESA-SUFICIENTEMENTE-LARGA-2026";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "MonrysApi";
var audience = builder.Configuration["Jwt:Audience"] ?? "MonrysApp";
var horas = int.TryParse(builder.Configuration["Jwt:HorasExpiracion"], out var h) ? h : 8;

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "name",
            RoleClaimType = "roles",
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// --------------------------------------------------------------------- CORS
// Nota: la sección de appsettings es un objeto {"Origenes": [...]}, por eso se lee "Cors:Origenes"
// (leer "Cors" como string[] produce un array vacío y deniega todos los orígenes).
var cors = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(opciones => opciones.AddPolicy("Front", politica => politica
    .WithOrigins(cors)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// ----------------------------------------------------------------- servicios
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttpContext>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IAuditoriaConsultaService, AuditoriaConsultaService>();
builder.Services.AddScoped<ICorrelativoService, CorrelativoService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IBancoService, BancoService>();
builder.Services.AddScoped<ITasaService, TasaService>();
builder.Services.AddScoped<IOperacionService, OperacionService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<ICustodioService, CustodioService>();
builder.Services.AddScoped<ITransferenciaService, TransferenciaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IInformeService, InformeService>();

builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opciones.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Monrys Capital API v2",
        Version = "v1",
        Description = "API REST del sistema de pagos/casa de cambio Monrys (migración desde Visual FoxPro)."
    });
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token JWT devuelto por POST /api/auth/login"
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ------------------------------------------------------------------- arranque
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonrysDbContext>();
    try
    {
        Console.WriteLine($"[Monrys] Inicializando base ({proveedor})...");
        Console.Out.Flush();
        await db.Database.EnsureCreatedAsync();
        DbSeeder.Sembrar(db);
        Console.WriteLine($"[Monrys] Base {proveedor} lista.");
    }
    catch (Exception ex)
    {
        errorBase = $"{ex.GetType().Name}: {ex.Message}";
        Console.WriteLine($"[Monrys] ERROR base {proveedor} -> {errorBase}");
    }
    Console.Out.Flush();
}

app.UseCors("Front");

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Monrys Capital API v2");
    opciones.DocumentTitle = "Monrys Capital API v2";
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async contexto =>
    {
        var excepcion = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (status, detalle) = excepcion switch
        {
            RecursoNoEncontradoException e => (StatusCodes.Status404NotFound, e.Message),
            ValidacionException e => (StatusCodes.Status400BadRequest, e.Message),
            ConflictoException e => (StatusCodes.Status409Conflict, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/problem+json";
        await contexto.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = detalle,
            Detail = excepcion is null ? null : excepcion.Message,
            Instance = contexto.Request.Path
        });
    });
});

app.UseAuthentication();
app.UseAuthorization();

// --------------------------------------------------------------- endpoint 404
app.MapGet("/", () => Results.Ok(new { status = "ok", api = "Monrys Capital API v2" }))
    .AllowAnonymous();

app.MapGet("/api/health", ([FromServices] IWebHostEnvironment env) => Results.Ok(new
{
    status = "ok",
    api = "Monrys Capital API v2",
    modo = proveedor,
    entorno = env.EnvironmentName,
    baseDatos = errorBase is null ? "ok" : "error",
    error = errorBase,
    pg = new { host = pgHost, port = pgPort, usuario = pgUser, passLen = pgPassLen, passSha = pgPassSha },
    fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
})).AllowAnonymous().WithTags("Health");

app.MapControllers();

app.Run();
