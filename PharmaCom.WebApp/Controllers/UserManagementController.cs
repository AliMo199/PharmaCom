using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaCom.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _signInManager = signInManager;
        }

        // GET: UserManagement/Index
        public async Task<IActionResult> Index(string searchTerm = null, string roleFilter = null, int pageNumber = 1, int pageSize = 10)
        {
            var query = _userManager.Users.AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(u =>
                    u.UserName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search));
            }

            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModels = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Get user orders
                var userOrders = await _unitOfWork.Order.FindAsync(o => o.ApplicationUserId == user.Id);
                var totalSpent = userOrders.Where(o => o.Status == "Completed" || o.Status == "Approved")
                    .Sum(o => o.TotalAmount);

                // Filter by role if specified
                if (!string.IsNullOrWhiteSpace(roleFilter) && !roles.Contains(roleFilter))
                    continue;

                userViewModels.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    Roles = roles.ToList(),
                    TotalOrders = userOrders.Count(),
                    TotalSpent = totalSpent
                });
            }

            ViewBag.TotalUsers = totalUsers;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(userViewModels);
        }

        // GET: UserManagement/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var orders = await _unitOfWork.Order.GetOrdersByUserIdAsync(id);
            var ordersList = orders.ToList();

            var viewModel = new UserDetailViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles.ToList(),
                Addresses = user.Addresses.ToList(),
                RecentOrders = ordersList.Take(10).Select(o => new OrderSummary
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount
                }).ToList(),
                Statistics = new UserStatistics
                {
                    TotalOrders = ordersList.Count,
                    TotalSpent = ordersList.Where(o => o.Status == "Completed" || o.Status == "Approved").Sum(o => o.TotalAmount),
                    AverageOrderValue = ordersList.Any() ? ordersList.Average(o => o.TotalAmount) : 0,
                    PendingOrders = ordersList.Count(o => o.Status == "Pending"),
                    CompletedOrders = ordersList.Count(o => o.Status == "Completed"),
                    CancelledOrders = ordersList.Count(o => o.Status == "Cancelled"),
                    LastOrderDate = ordersList.Any() ? ordersList.Max(o => o.OrderDate) : (DateTime?)null
                }
            };

            return View(viewModel);
        }

        // GET: UserManagement/Create
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.AvailableRoles = roles;
            return View(new AdminRegisterViewModel());
        }

        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = model.EmailConfirmed,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed
            };

            // Add address if provided
            if (!string.IsNullOrEmpty(model.Line1))
            {
                user.Addresses.Add(new Address
                {
                    Line1 = model.Line1,
                    City = model.City,
                    Governorate = model.Governorate,
                    IsDefault = true
                });
            }

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign roles
                if (model.SelectedRoles != null && model.SelectedRoles.Any())
                {
                    await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                }

                TempData["Success"] = $"User '{user.UserName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // GET: UserManagement/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                SelectedRoles = roles.ToList()
            };

            ViewBag.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(viewModel);
        }

        // POST: UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Update user properties
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.EmailConfirmed = model.EmailConfirmed;
            user.PhoneNumberConfirmed = model.PhoneNumberConfirmed;
            user.TwoFactorEnabled = model.TwoFactorEnabled;
            user.LockoutEnabled = model.LockoutEnabled;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(model.SelectedRoles).ToList();
                var rolesToAdd = model.SelectedRoles.Except(currentRoles).ToList();

                if (rolesToRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                if (rolesToAdd.Any())
                    await _userManager.AddToRolesAsync(user, rolesToAdd);

                TempData["Success"] = $"User '{user.UserName}' updated successfully!";
                return RedirectToAction(nameof(Details), new { id = user.Id });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // GET: UserManagement/ChangePassword/5
        public async Task<IActionResult> ChangePassword(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var viewModel = new ChangeUserPasswordViewModel
            {
                UserId = user.Id,
                UserName = user.UserName
            };

            return View(viewModel);
        }

        // POST: UserManagement/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangeUserPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            // Remove old password and set new one
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            var addResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (addResult.Succeeded)
            {
                TempData["Success"] = $"Password changed successfully for user '{user.UserName}'!";
                return RedirectToAction(nameof(Details), new { id = user.Id });
            }

            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string id, int lockoutDays = 7)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid user ID" });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // Don't allow locking yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == id)
            {
                return Json(new { success = false, message = "You cannot lock your own account" });
            }

            // Set lockout end date
            var lockoutEnd = DateTimeOffset.UtcNow.AddDays(lockoutDays);

            // Enable lockout for this user if not already enabled
            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (result.Succeeded)
            {
                return Json(new
                {
                    success = true,
                    message = $"User '{user.UserName}' locked until {lockoutEnd.LocalDateTime:MMM dd, yyyy}",
                    lockoutEnd = lockoutEnd.ToString("o") // ISO format for JavaScript
                });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Failed to lock user: {errors}" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid user ID" });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // Remove lockout
            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded)
            {
                // Reset access failed count
                await _userManager.ResetAccessFailedCountAsync(user);

                return Json(new
                {
                    success = true,
                    message = $"User '{user.UserName}' unlocked successfully"
                });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Failed to unlock user: {errors}" });
        }

        // POST: UserManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // Prevent deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == id)
            {
                return Json(new { success = false, message = "You cannot delete your own account" });
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User deleted successfully" });
            }

            return Json(new { success = false, message = "Failed to delete user" });
        }

        // GET: UserManagement/ExportUsers
        public async Task<IActionResult> ExportUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Username,Email,First Name,Last Name,Phone,Email Confirmed,Roles,Total Orders,Total Spent");

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var orders = await _unitOfWork.Order.FindAsync(o => o.ApplicationUserId == user.Id);
                var totalSpent = orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount);

                csv.AppendLine($"{user.UserName},{user.Email},{user.FirstName},{user.LastName},{user.PhoneNumber},{user.EmailConfirmed},{string.Join(";", roles)},{orders.Count()},{totalSpent}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Users_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}