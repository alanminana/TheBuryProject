using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using TheBuryProject.Data;

namespace TheBuryProject.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El email es requerido")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "La contraseña es requerida")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Recordarme")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Limpiar cookies existentes
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Intento de login: Email={Email}", Input.Email);

                // Buscar usuario
                var user = await _userManager.FindByEmailAsync(Input.Email);
                
                if (user == null)
                {
                    _logger.LogWarning("Usuario no encontrado: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Usuario o contraseña inválidos.");
                    return Page();
                }

                // Verificar que esté activo
                if (!user.Activo)
                {
                    _logger.LogWarning("Usuario inactivo intentó login: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Esta cuenta ha sido desactivada. Contacte al administrador.");
                    return Page();
                }

                // Verificar password
                var passwordOk = await _userManager.CheckPasswordAsync(user, Input.Password);
                if (!passwordOk)
                {
                    _logger.LogWarning("Password incorrecta para: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Usuario o contraseña inválidos.");
                    
                    // Incrementar contador de intentos fallidos
                    await _userManager.AccessFailedAsync(user);
                    return Page();
                }

                // Intentar sign in
                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario logueado: {Email}", Input.Email);
                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("Requiere 2FA: {Email}", Input.Email);
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Usuario bloqueado: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Esta cuenta ha sido bloqueada. Contacte al administrador.");
                    return Page();
                }

                if (result.IsNotAllowed)
                {
                    _logger.LogWarning("Login no permitido (email no confirmado?): {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Debe confirmar su email antes de iniciar sesión.");
                    return Page();
                }

                _logger.LogWarning("Login falló por razón desconocida: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Intento de login inválido.");
                return Page();
            }

            // Si llegamos aquí, algo falló, volver a mostrar formulario
            return Page();
        }
    }
}
