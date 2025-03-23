using System.Security.Claims;
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

    [HttpGet]
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
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(Login model, string? returnUrl = null)
    {

        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {

            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user != null && model.Password != null)
            {

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                var userId = _signInManager.Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (result.Succeeded && userId != null && user.Email != null)

                {
                    string access_token = _tokenGenerator.GeneratorToken(user.Email, userId);

                    _logger.LogInformation("User logged in.");

                    Response.Cookies.Append("JwtToken", access_token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(3)
                    });

                    TempData["Success"] = "Login Successful!";

                    return RedirectToLocal(returnUrl!);
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    _logger.LogInformation("User Failed to login.");

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }
        }
        return View(model);

    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout()
    {
        return View();
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

                    var access_token = _tokenGenerator.GeneratorToken(user.Email!, user.Id);
                    Response.Cookies.Append("JwtToken", access_token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(3)
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