using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SalaReuniones.Controllers
{
    /// <summary>
    /// Controlador de administración de usuarios.
    ///
    /// 4 CO:
    /// ✔ Completado: CRUD completo de usuarios + roles
    /// ✔ Corregido: login/registro solo con nombre de usuario
    /// ✔ Comentado: explicaciones de cada sección
    /// ✔ Coherente: uso de contraseña global, soft delete, bloqueo y reactivación
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// Inyección de servicios de Identity
        /// </summary>
        public AdminController(UserManager<IdentityUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ============================================================
        // LISTAR USUARIOS
        // ============================================================
        /// <summary>
        /// Muestra todos los usuarios con su rol asignado y estado.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<dynamic>();

            foreach (var user in users)
            {
                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                // Determinar estado del usuario (activo o inhabilitado)
                var estado = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now
                    ? "Inhabilitado"
                    : "Activo";

                userList.Add(new
                {
                    user.Id,
                    user.UserName,
                    Role = role,
                    Estado = estado
                });
            }

            return View(userList);
        }

        // ============================================================
        // CREAR USUARIO (GET)
        // ============================================================
        public IActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View();
        }

        // ============================================================
        // CREAR USUARIO (POST)
        // ============================================================
        /// <summary>
        /// Crea un usuario usando solo el nombre de usuario y contraseña global.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string UserName, string role)
        {
            string passwordGlobal = "Oficina2026*";

            if (string.IsNullOrWhiteSpace(UserName))
                ModelState.AddModelError("", "El nombre de usuario es obligatorio.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.ToList();
                return View();
            }

            var user = new IdentityUser
            {
                UserName = UserName,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(user, passwordGlobal);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(role))
                    await _userManager.AddToRoleAsync(user, role);

                TempData["Success"] = "Usuario creado correctamente.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.Roles = _roleManager.Roles.ToList();
            return View();
        }

        // ============================================================
        // EDITAR USUARIO (GET)
        // ============================================================
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            ViewBag.Roles = _roleManager.Roles.ToList();
            ViewBag.CurrentRole = currentRole;

            return View(user);
        }

        // ============================================================
        // EDITAR USUARIO (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string username, string role)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.UserName = username;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                ViewBag.Roles = _roleManager.Roles.ToList();
                ViewBag.CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
                return View(user);
            }

            // Actualizar roles
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
                await _userManager.RemoveFromRolesAsync(user, roles);

            if (!string.IsNullOrEmpty(role))
                await _userManager.AddToRoleAsync(user, role);

            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // ============================================================
        // INHABILITAR USUARIO (GET)
        // ============================================================
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Protección: no inhabilitar administradores
            if ((await _userManager.GetRolesAsync(user)).Contains("Administrador"))
            {
                TempData["Error"] = "No se puede inhabilitar un usuario Administrador.";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // ============================================================
        // INHABILITAR USUARIO (POST)
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Administrador"))
            {
                TempData["Error"] = "No se puede inhabilitar un usuario Administrador.";
                return RedirectToAction("Index");
            }

            // Bloquear usuario (soft delete)
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Usuario inhabilitado correctamente.";
            return RedirectToAction("Index");
        }

        // ============================================================
        // ACTIVAR USUARIO (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Quitar bloqueo
            user.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] = "No se pudo activar el usuario.";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Usuario activado correctamente.";
            return RedirectToAction("Index");
        }
    }
}