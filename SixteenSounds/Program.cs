using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SixteenSounds.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REJESTRACJA SERWISÓW (Dependency Injection) ---

builder.Services.AddControllers();

// Konfiguracja Bazy Danych
builder.Services.AddDbContext<SixteenSoundsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Konfiguracja CORS (Pozwala Twojej stronie index.html gadaæ z API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// KONFIGURACJA JWT (Bezpieczeñstwo)
// Pobieramy klucz z appsettings.json, a jeœli go nie ma - u¿ywamy zapasowego
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TwojBaaaaardzoDlugiKluczZastepczyMinimum32Znaki_123!";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false, // Mo¿esz zmieniæ na true i ustawiæ w appsettings
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero // Token wygasa dok³adnie wtedy, kiedy mu ka¿emy
    };
});

// Konfiguracja Swaggera (z obs³ug¹ k³ódki dla JWT)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SixteenSounds API", Version = "v1" });

    // Dodaje przycisk "Authorize" do Swaggera, ¿ebyœ móg³ testowaæ tokeny
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Wklej tutaj swój token JWT: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        new string[] { }
    }});
});

var app = builder.Build();

// --- 2. PIPELINE (Kolejnoœæ ma znaczenie!) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Pozwala serwerowi wysy³aæ pliki (muzyka, obrazy, html)
app.UseStaticFiles();

// W³¹czamy politykê CORS przed autentykacj¹
app.UseCors("AllowAll");

// NAJWA¯NIEJSZA KOLEJNOŒÆ: Najpierw Auth, potem Auth!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Automatyczna migracja bazy przy starcie
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SixteenSoundsDbContext>();
    context.Database.Migrate();
}

Console.WriteLine(">>> SIXTEEN SOUNDS SERVER STARTED SUCCESSFULLY <<<");
app.Run();