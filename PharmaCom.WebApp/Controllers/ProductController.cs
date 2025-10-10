using Microsoft.AspNetCore.Mvc;
using PharmaCom.DataInfrastructure.Implementation;
using Pharm
using PharmaCom.Domain.Repositories;

namespace PharmaCom.WebApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;

        public ProductController(IUnitOfWork unitOfWork)
        {
            _UnitOfWork = unitOfWork;
        }

        // GET: /Product
        public async Task<IActionResult> Index()
        {
            var products = await _UnitOfWork.Product.GetAllAsync();
            return View(products);
        }

        // GET: /Product/Details
        public async Task<IActionResult> Details(int id)
        {
            var product = await _UnitOfWork.Product.GetProductWithCategoryAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // GET: /Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                await _UnitOfWork.Product.AddAsync(product);
                _UnitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: /Product/Edit
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _UnitOfWork.Product.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: /Product/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                _UnitOfWork.Product.Update(product);
                _UnitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: /Product/Delete
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _UnitOfWork.Product.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: /Product/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            var product = await _UnitOfWork.Product.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            _UnitOfWork.Product.Remove(product);
            _UnitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
    }
}
