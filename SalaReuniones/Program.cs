using SalaReuniones.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// CONFIGURACIÓN DE LICENCIA QUESTPDF
// ======================================================
// Necesario para evitar excepción en ejecución.
// Community es gratuita para uso no comercial/empresas pequeñas.
QuestPDF.Settings.License = LicenseType.Community;


// ======================================================
// SERVICIOS
// ======================================================

// -----------------------------
// MVC (Controladores + Vistas)
// -----------------------------
builder.Services.AddControllersWithViews();

// -----------------------------
// DbContext con SQL Server
// -----------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// -----------------------------
// Identity + Roles
// -----------------------------
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // No requiere confirmación por email
    options.SignIn.RequireConfirmedAccount = false;

    // Configuración de contraseña
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>() // Habilita roles
.AddEntityFrameworkStores<ApplicationDbContext>();

// ======================================================
// CONFIGURACIÓN DE COOKIE DE AUTENTICACIÓN
// ======================================================
// Controla duración de sesión y seguridad
builder.Services.ConfigureApplicationCookie(options =>
{
    // Tiempo máximo sin actividad
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

    // Renueva la cookie si el usuario sigue activo
    options.SlidingExpiration = true;

    // Rutas de seguridad
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Seguridad adicional
    options.Cookie.HttpOnly = true;
});


// ======================================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ======================================================
var app = builder.Build();


// ======================================================
// PIPELINE HTTP
// ======================================================

// Manejo de errores en producción
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Seguridad adicional HTTPS
}

// Redirección HTTPS
app.UseHttpsRedirection();

// Archivos estáticos (css, js, imágenes)
app.UseStaticFiles();

// Routing
app.UseRouting();

// ------------------------------------------------------
// Identity Middleware
// ------------------------------------------------------

// 1️⃣ Autenticación (Login)
app.UseAuthentication();

// 2️⃣ Autorización (Roles y permisos)
app.UseAuthorization();


// ======================================================
// MAPEO DE RUTAS
// ======================================================

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reservas}/{action=Index}/{id?}"
);

// Rutas para Identity (Login, Register, etc.)
app.MapRazorPages();


// ======================================================
// INICIALIZAR ROLES Y USUARIOS
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    //  Contraseña global inicial
    string passwordGlobal = "Oficina2026*";

    // Roles del sistema
    string[] roles = { "Administrador", "Usuario", "Visualizador" };

    // Crear roles si no existen
    foreach (var role in roles)
    {
        if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(role))
                .GetAwaiter().GetResult();

            Console.WriteLine($"Rol creado: {role}");
        }
    }

    // Método para crear usuarios iniciales
    void CreateUserIfNotExists(string email, string role)
    {
        var user = userManager.FindByEmailAsync(email)
            .GetAwaiter().GetResult();

        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = userManager.CreateAsync(user, passwordGlobal)
                .GetAwaiter().GetResult();

            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(user, role)
                    .GetAwaiter().GetResult();

                Console.WriteLine($"Usuario creado: {email} con rol {role}");
            }
        }
    }

    // Usuarios iniciales del sistema
    CreateUserIfNotExists("admin@empresa.com", "Administrador");
    CreateUserIfNotExists("usuario@empresa.com", "Usuario");
    CreateUserIfNotExists("visor@empresa.com", "Visualizador");
}


// ======================================================
// EJECUCIÓN DE LA APLICACIÓN
// ======================================================
app.Run();