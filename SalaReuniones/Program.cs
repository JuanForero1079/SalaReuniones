using SalaReuniones.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// =============================
// SERVICIOS
// =============================

// MVC (Controladores + Vistas)
builder.Services.AddControllersWithViews();

// Configuración de DbContext con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // No requiere confirmación de cuenta al registrarse
    options.SignIn.RequireConfirmedAccount = false;

    // Configuración de contraseñas (opcional)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>() // Habilita roles (Administrador, Usuario, Visualizador)
.AddEntityFrameworkStores<ApplicationDbContext>();

// =============================
// CONSTRUCCIÓN DE LA APP
// =============================
var app = builder.Build();

// =============================
// PIPELINE HTTP
// =============================

// Manejo de errores y HSTS en producción
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirección HTTPS y archivos estáticos
app.UseHttpsRedirection();
app.UseStaticFiles();

// Routing
app.UseRouting();

// ===== Identity =====

// Primero: Authentication (login)
app.UseAuthentication();

// Segundo: Authorization (roles, permisos)
app.UseAuthorization();

// =============================
// MAPEO DE RUTAS
// =============================

// Ruta por defecto: Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reservas}/{action=Index}/{id?}");

// Mapear páginas de Identity (Login, Register, Logout, Manage, etc.)
app.MapRazorPages();

// =============================
// INICIALIZAR ROLES Y USUARIOS
// =============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    //  Contraseña global para toda la oficina
    string passwordGlobal = "Oficina2026*";

    string[] roles = { "Administrador", "Usuario", "Visualizador" };

    foreach (var role in roles)
    {
        if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            Console.WriteLine($"Rol creado: {role}");
        }
    }

    void CreateUserIfNotExists(string email, string role)
    {
        var user = userManager.FindByEmailAsync(email).GetAwaiter().GetResult();

        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = userManager.CreateAsync(user, passwordGlobal).GetAwaiter().GetResult();

            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(user, role).GetAwaiter().GetResult();
                Console.WriteLine($"Usuario creado: {email} con rol {role}");
            }
        }
    }

    // Crear usuarios iniciales
    CreateUserIfNotExists("admin@empresa.com", "Administrador");
    CreateUserIfNotExists("usuario@empresa.com", "Usuario");
    CreateUserIfNotExists("visor@empresa.com", "Visualizador");
}

// =============================
// EJECUCIÓN
// =============================
app.Run();
