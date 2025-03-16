using Application.Data;
using c_Backend.Controllers;
using Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users.Models;
using Users.Models.AccountViews;

namespace Users.Controllers;

public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenGenerator _tokenGenerator;
    private readonly ILogger _logger;

    public UsersController(
        UserManager<User> userManager, SignInManager<User> signInManager,
        ApplicationDbContext context,
        TokenGenerator tokenGenerator,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _tokenGenerator = tokenGenerator;
        _logger = logger;

    }
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
    // Login
    [HttpPost]
    public async Task<IActionResult> Login(Login model, string? returnUrl = null)
    {

        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            if (!string.IsNullOrEmpty(model.Email))
            {

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password!, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {

                    string access_token = _tokenGenerator.GeneratorToken(model.Email, model.Password!);

                    _logger.LogInformation("User logged in.");

                    Response.Cookies.Append("JwtToken", access_token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(1)
                    });

                    TempData["Success"] = "Login Successful!";

                    return RedirectToLocal(returnUrl!);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }
        return View(model);

    }


    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(Register model, string? returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            var user = new User { UserName = model.UserName, Email = model.Email };
            if (!string.IsNullOrEmpty(model.Password))
            {
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    var access_token = _tokenGenerator.GeneratorToken(user.Email!, model.Password!);
                    Response.Cookies.Append("JwtToken", access_token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(1)
                    });

                    TempData["Success"] = "Login Successful!";

                    return RedirectToLocal(returnUrl!);

                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");

        TempData["Fail"] = "You are Logged out!!.";

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
    public IActionResult ForgotPassword()
    {
        return View();
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}