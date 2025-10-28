using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.ViewModels;
using System.Security.Claims;

namespace PharmaCom.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        } 

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            if (!ModelState.IsValid) return View(registerViewModel);

            var user = new ApplicationUser
            {
                UserName = registerViewModel.UserName,
                FirstName = registerViewModel.FirstName,
                LastName = registerViewModel.LastName,
                Email = $"{registerViewModel.UserName}@pharma.com"
            };

          
            if (!string.IsNullOrEmpty(registerViewModel.Line1))
            {
                user.Addresses.Add(new Address
                {
                    Line1 = registerViewModel.Line1,
                    City = registerViewModel.City,
                    Governorate = registerViewModel.Governorate
                });
            }

            var res = await _userManager.CreateAsync(user, registerViewModel.Password);
            if (res.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Employee");
            }

            foreach (var item in res.Errors)
                ModelState.AddModelError("", item.Description);

            return View(registerViewModel);
        }
        #endregion

        #region SignOut
        public async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        #endregion

        #region Login
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LogInViewModel logInViewModel)
        {
            if (!ModelState.IsValid) return View(logInViewModel);

            var appUser = await _userManager.FindByNameAsync(logInViewModel.Name);
            if (appUser != null)
            {
                bool Found = await _userManager.CheckPasswordAsync(appUser, logInViewModel.Password);
                if (Found)
                {
                    var Claims = new List<Claim>
                    {
                        new Claim("FullName", appUser.FullName)
                    };

                    await _signInManager.SignInWithClaimsAsync(appUser, logInViewModel.RememberMe, Claims);
                    return RedirectToAction("Index", "Employee");
                }
            }

            ModelState.AddModelError("", "Name or pass is not right!");
            return View(logInViewModel);
        }
        #endregion
    }
}



