using Microsoft.EntityFrameworkCore;
using UGEL_BoletasWeb.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. ZONA DE SERVICIOS (LA CAJA DE HERRAMIENTAS ESTÁ ABIERTA)
// ====================================================================
builder.Services.AddControllersWithViews();

// A. Inyectar Base de Datos ANTES del Build
builder.Services.AddDbContext<UgelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UgelConexion")));

// B. Inyectar el Motor Parser ANTES del Build
builder.Services.AddScoped<UGEL_BoletasWeb.Services.Parser.IMotorProcesadorLis, UGEL_BoletasWeb.Services.Parser.MotorProcesadorLis>();

// ====================================================================
// EL PUNTO DE NO RETORNO: CONSTRUIR LA APLICACIÓN
// ====================================================================
var app = builder.Build();

// ====================================================================
// 2. ZONA DE MIDDLEWARES (LA APLICACIÓN ESTÁ CORRIENDO)
// ====================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();