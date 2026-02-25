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
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Contraseña global de la oficina
            string passwordGlobal = "Oficina2026*";

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                passwordGlobal,
                false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario inició sesión correctamente.");
                return LocalRedirect(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Correo no válido.");
            return Page();
        }
    }
}