using SalaReuniones.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient; // Para obtener info de la conexión

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Aquí agregamos la conexión a la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// === Mostramos info de la conexión EF Core al iniciar ===
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var connection = context.Database.GetDbConnection();
    Console.WriteLine("=== Conexión EF Core ===");
    Console.WriteLine($"Servidor: {connection.DataSource}");
    Console.WriteLine($"Base de datos: {connection.Database}");
    Console.WriteLine($"Estado de conexión: {connection.State}");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
