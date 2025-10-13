using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PharmaCom.Domain.Models;
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
            var products = await _UnitOfWork.Product.GetAllProductWithCategoryAsync();
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

        #region Create
        // GET: /Product/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_UnitOfWork.Category.GetAllAsync().Result, "Id", "Name");
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
            ViewBag.Categories = new SelectList(_UnitOfWork.Category.GetAllAsync().Result, "Id", "Name");
            return View(product);
        }
        #endregion

        #region Edit
        // GET: /Product/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _UnitOfWork.Product.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            ViewBag.Categories = new SelectList(await _UnitOfWork.Category.GetAllAsync(), "Id", "Name", product.CategoryId);
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

        //// POST: /Product/Edit (ChatGPT enhanced with image upload)
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, Product model, IFormFile ImageFile)
        //{
        //    if (id != model.Id)
        //        return BadRequest();

        //    if (ModelState.IsValid)
        //    {
        //        if (ImageFile != null && ImageFile.Length > 0)
        //        {
        //            var fileName = Path.GetFileName(ImageFile.FileName);
        //            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", fileName);

        //            using (var stream = new FileStream(filePath, FileMode.Create))
        //            {
        //                await ImageFile.CopyToAsync(stream);
        //            }

        //            model.ImageURLString = "/images/products/" + fileName;
        //        }

        //        _UnitOfWork.Product.Update(model);
        //        _UnitOfWork.Save();

        //        return RedirectToAction(nameof(Index));
        //    }

        //    ViewBag.Categories = new SelectList(await _UnitOfWork.Category.GetAllAsync(), "Id", "Name", model.CategoryId);
        //    return View(model);
        //}
        #endregion

        #region Delete
        // GET: /Product/Delete
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _UnitOfWork.Product.GetByIdAsync(id);
            if (product == null)
                return NotFound();
            else
            {
                _UnitOfWork.Product.Remove(product);
                _UnitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion
    }
}
