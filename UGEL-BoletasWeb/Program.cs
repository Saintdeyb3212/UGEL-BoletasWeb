using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting; // 🚀 Requerido para blindar el tráfico
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using UGEL_BoletasWeb.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. ZONA DE SERVICIOS (POLÍTICAS DE SEGURIDAD ACTIVADAS)
// ====================================================================
builder.Services.AddControllersWithViews();

// A. Conexión a Base de Datos
builder.Services.AddDbContext<UgelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UgelConexion")));

// B. Motores e Interfaces del Dominio
builder.Services.AddScoped<UGEL_BoletasWeb.Services.Parser.IMotorProcesadorLis, UGEL_BoletasWeb.Services.Parser.MotorProcesadorLis>();
builder.Services.AddScoped<UGEL_BoletasWeb.Services.PdfExport.IGeneradorBoletaPdf, UGEL_BoletasWeb.Services.PdfExport.GeneradorBoletaPdf>();

// C. 🚀 BLINDAJE DE COOKIES CONTRA BRECHAS DE SEGURIDAD
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Autenticacion/IniciarSesion";
        options.AccessDeniedPath = "/Autenticacion/IniciarSesion";

        // Configuración de Seguridad Avanzada en Cookies
        options.Cookie.HttpOnly = true; // Impide que atacantes roben la sesión mediante scripts inyectados (XSS)
        options.Cookie.SameSite = SameSiteMode.Strict; // Bloquea el envío de la cookie desde sitios externos (Anti-CSRF)
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // En producción forzar Always para HTTPS

        // Tiempo de vida máximo estricto de la sesión
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Expira exactamente en 8 horas de jornada laboral
        options.SlidingExpiration = false; // 🚀 CRÍTICO: Desactivado. Cumplidas las 8 horas el token muere obligatoriamente, forzando un re-login seguro
    });

// D. 🚀 BLINDAJE ANTI-SATURACIÓN DE TRÁFICO (RATE LIMITING)
// Evita que un bucle en el cliente o un ataque automatizado mate el servidor físico de la oficina
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política por dirección IP fija
    options.AddPolicy("FiltroTraficoIP", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60, // Máximo 60 peticiones...
                Window = TimeSpan.FromMinutes(1), // ...por minuto por cada dirección IP
                QueueLimit = 2 // Permite una cola mínima para solicitudes legítimas pesadas
            }));
});

// ====================================================================
// EL PUNTO DE NO RETORNO: CONSTRUIR LA APLICACIÓN
// ====================================================================
var app = builder.Build();

// ====================================================================
// 2. ZONA DE MIDDLEWARES (ORDEN DE EJECUCIÓN CRÍTICO)
// ====================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🚀 ACTIVAR LIMITADOR DE TRÁFICO ANTES DE LA AUTENTICACIÓN
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacion}/{action=IniciarSesion}/{id?}");

app.Run();