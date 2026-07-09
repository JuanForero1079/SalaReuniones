#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace SalaReuniones.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Nombre de usuario")]
            public string UserName { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= string.Empty;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Contraseña global de la oficina
            string passwordGlobal = "Oficina2026*";

            var result = await _signInManager.PasswordSignInAsync(
                Input.UserName,
                passwordGlobal,
                false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario inició sesión correctamente.");

                // Si el usuario intentó acceder a una página protegida,
                // regresar allí.
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                // En un inicio de sesión normal,
                // llevar directamente al calendario.
                return RedirectToAction(
                    "Calendar",
                    "Reservas");
            }

            ModelState.AddModelError(string.Empty, "Usuario no válido.");
            return Page();
        }
    }
}