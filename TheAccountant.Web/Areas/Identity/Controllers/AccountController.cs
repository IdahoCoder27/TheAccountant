using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TheAccountant.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Route("Identity/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { area = "Identity", returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return RedirectToAction("Login", "Account"); // or show error page
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login", "Account"); // or show error page
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                // Optional: update any user data here (e.g., last login)
                return LocalRedirect(returnUrl ?? "/Identity/Dashboard/Index");
            }
            else
            {
                // If the user does not have an account, you may want to create one
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);

                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email
                };

                var createResult = await _userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    createResult = await _userManager.AddLoginAsync(user, info);
                    if (createResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl ?? "/");
                    }
                }

                // Handle creation errors
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return RedirectToAction("Login", "Account"); // or show error view
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
