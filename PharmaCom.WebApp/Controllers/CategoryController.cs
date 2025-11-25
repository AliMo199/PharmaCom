using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;

namespace PharmaCom.WebApp.Controllers
{
    [Authorize(Roles = "Admin, Pharmacist")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<IActionResult> Index()
        {
            var categories = await _unitOfWork.Category.GetAllAsync();
            return View(categories);
        }


        public async Task<IActionResult> Details(int id)
        {
            var category = await _unitOfWork.Category.GetCategoryWithProductsAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                await _unitOfWork.Category.AddAsync(category);
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _unitOfWork.Category.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(category);
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _unitOfWork.Category.GetByIdAsync(id);
            if (category == null)
                return NotFound();
            else
            {
                _unitOfWork.Category.Remove(category);
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
