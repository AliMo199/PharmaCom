using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.ViewModels;
using PharmaCom.WebApp.Mappings;
using System.Security.Claims;

namespace PharmaCom.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMapper _mapper;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
        }

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
                Email = registerViewModel.Email,
                PhoneNumber = registerViewModel.PhoneNumber
            };


            if (!string.IsNullOrEmpty(registerViewModel.Line1))
            {
                var Address = new Address
                {
                    Line1 = registerViewModel.Line1,
                    City = registerViewModel.City,
                    Governorate = registerViewModel.Governorate
                };
                user.Addresses.Add(Address);
            }

            var res = await _userManager.CreateAsync(user, registerViewModel.Password);
            if (res.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var item in res.Errors)
                ModelState.AddModelError("", item.Description);

            return View(registerViewModel);
        }

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
                    return RedirectToAction("Store", "Home");
                }
            }

            ModelState.AddModelError("", "UserName or Password is not right!");
            return View(logInViewModel);
        }

        public async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // GET: Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Load addresses
            var userWithAddresses = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            return View(userWithAddresses);
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                TempData["Error"] = "First name and last name are required.";
                return RedirectToAction(nameof(Profile));
            }

            // Update user properties
            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();
            user.PhoneNumber = phoneNumber?.Trim();

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to update profile: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Profile));
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "All password fields are required.";
                return RedirectToAction(nameof(Profile));
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirmation password do not match.";
                return RedirectToAction(nameof(Profile));
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long.";
                return RedirectToAction(nameof(Profile));
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password changed successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to change password: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Profile));
        }

        // GET: Account/AddAddress
        [HttpGet]
        public IActionResult AddAddress()
        {
            return View();
        }

        // POST: Account/AddAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address address)
        {
            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Remove ApplicationUserId from ModelState validation since we'll set it manually
            ModelState.Remove("ApplicationUserId");

            if (!ModelState.IsValid)
            {
                return View(address);
            }

            // Set the ApplicationUserId to the current user
            address.ApplicationUserId = user.Id;

            // If this is the first address or marked as default, set it as default
            if (!user.Addresses.Any() || address.IsDefault)
            {
                // Set all other addresses to non-default
                foreach (var addr in user.Addresses)
                {
                    addr.IsDefault = false;
                }
                address.IsDefault = true;
            }

            user.Addresses.Add(address);
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Address added successfully!";
                return RedirectToAction(nameof(Profile));
            }

            TempData["Error"] = "Failed to add address.";
            return View(address);
        }

        // GET: Account/EditAddress
        [HttpGet]
        public async Task<IActionResult> EditAddress(int id)
        {
            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));


            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var address = user.Addresses.FirstOrDefault(a => a.Id == id);
            if (address == null)
            {
                TempData["Error"] = "Address not found.";
                return RedirectToAction(nameof(Profile));
            }

            return View(address);
        }

        // POST: Account/EditAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(Address address)
        {
            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("ApplicationUserId");
            if (!ModelState.IsValid)
            {
                return View(address);
            }

            var existingAddress = user.Addresses.FirstOrDefault(a => a.Id == address.Id);
            if (existingAddress == null)
            {
                TempData["Error"] = "Address not found.";
                return RedirectToAction(nameof(Profile));
            }

            // Update properties
            existingAddress.Line1 = address.Line1;
            existingAddress.City = address.City;
            existingAddress.Governorate = address.Governorate;

            // If setting as default, unset other defaults
            if (address.IsDefault && !existingAddress.IsDefault)
            {
                foreach (var addr in user.Addresses.Where(a => a.Id != address.Id))
                {
                    addr.IsDefault = false;
                }
                existingAddress.IsDefault = true;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Address updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            TempData["Error"] = "Failed to update address.";
            return View(address);
        }

        // POST: Account/DeleteAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var address = user.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address == null)
            {
                return Json(new { success = false, message = "Address not found" });
            }

            // If deleting the default address, set another as default
            bool wasDefault = address.IsDefault;
            user.Addresses.Remove(address);

            if (wasDefault && user.Addresses.Any())
            {
                user.Addresses.First().IsDefault = true;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Address deleted successfully" });
            }

            return Json(new { success = false, message = "Failed to delete address" });
        }

        // POST: Account/SetDefaultAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var address = user.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address == null)
            {
                return Json(new { success = false, message = "Address not found" });
            }

            // Set all addresses to non-default
            foreach (var addr in user.Addresses)
            {
                addr.IsDefault = false;
            }

            // Set the selected address as default
            address.IsDefault = true;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Default address updated successfully" });
            }

            return Json(new { success = false, message = "Failed to set default address" });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyProfileJson()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            //AUTOMAPPER HERE
            var userDto = _mapper.Map<UserProfileDto>(user);

            return Json(new
            {
                success = true,
                data = userDto,
                message = "Profile data retrieved successfully using AutoMapper"
            });
        }
    }
}



