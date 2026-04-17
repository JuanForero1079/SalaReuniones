using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SalaReuniones.ViewModels;

namespace SalaReuniones.Controllers
{
    /// <summary>
    /// Controlador de administración de usuarios
    /// SOLO accesible para Administradores
    ///
    /// 4CO:
    /// ✔ Completo: CRUD de usuarios con roles
    /// ✔ Corregido: sin dynamic y mejor manejo de Identity
    /// ✔ Comentado: cada sección explicada
    /// ✔ Coherente: búsqueda, filtros, orden y paginación
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ============================================================
        // LISTAR USUARIOS
        // BUSQUEDA + FILTRO + ORDEN + PAGINACION
        // ============================================================
        public async Task<IActionResult> Index(string search, string roleFilter, int page = 1)
        {
            int pageSize = 10;

            var users = _userManager.Users.ToList();
            var userList = new List<UserAdminViewModel>();

            // ===============================
            // CONSTRUIR VIEWMODEL
            // ===============================
            foreach (var user in users)
            {
                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                var estado = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now
                    ? "Inhabilitado"
                    : "Activo";

                userList.Add(new UserAdminViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Role = role ?? "",
                    Estado = estado
                });
            }

            // ===============================
            // BUSQUEDA POR USERNAME
            // ===============================
            if (!string.IsNullOrWhiteSpace(search))
            {
                userList = userList
                    .Where(u => u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // ===============================
            // FILTRO POR ROL
            // ===============================
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                userList = userList
                    .Where(u => u.Role == roleFilter)
                    .ToList();
            }

            // ===============================
            // ORDENAMIENTO
            // Primero por ROL
            // Luego por USERNAME
            // ===============================
            userList = userList
                .OrderBy(u => u.Role)
                .ThenBy(u => u.UserName)
                .ToList();

            // ===============================
            // PAGINACION
            // ===============================
            int totalUsers = userList.Count;

            var usersPage = userList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new UserListViewModel
            {
                Users = usersPage,
                Search = search ?? "",
                RoleFilter = roleFilter ?? "",
                Page = page,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize)
            };

            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            return View(model);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string role)
        {
            string passwordGlobal = "Oficina2026*";

            if (string.IsNullOrWhiteSpace(username))
                ModelState.AddModelError("", "El nombre de usuario es obligatorio.");

            var existingUser = await _userManager.FindByNameAsync(username);

            if (existingUser != null)
                ModelState.AddModelError("", "Ya existe un usuario con ese nombre.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.ToList();
                return View();
            }

            var user = new IdentityUser
            {
                UserName = username,
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

            // ===============================
            // VALIDAR DUPLICADOS
            // ===============================
            var existingUser = await _userManager.FindByNameAsync(username);

            if (existingUser != null && existingUser.Id != id)
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese nombre.");

                ViewBag.Roles = _roleManager.Roles.ToList();
                ViewBag.CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
                return View(user);
            }

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

            // ===============================
            // ACTUALIZAR ROLES
            // ===============================
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

            if ((await _userManager.GetRolesAsync(user)).Contains("Administrador"))
            {
                TempData["Error"] = "No se puede inhabilitar un Administrador.";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // ============================================================
        // SOFT DELETE
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
                TempData["Error"] = "No se puede inhabilitar un Administrador.";
                return RedirectToAction("Index");
            }

            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Usuario inhabilitado correctamente.";

            return RedirectToAction("Index");
        }

        // ============================================================
        // ACTIVAR USUARIO
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

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