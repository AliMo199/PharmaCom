using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Domain.ViewModels;
using PharmaCom.Service.Interfaces;
using System.Security.Claims;

namespace PharmaCom.WebApp.Controllers
{
    public class PrescriptionController : Controller
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly IOrderService _orderService;

        public PrescriptionController(
            IPrescriptionService prescriptionService,
            IOrderService orderService)
        {
            _prescriptionService = prescriptionService;
            _orderService = orderService;
        }

        [HttpGet]
        //[Authorize]
        public IActionResult Upload(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Upload(IFormFile file, int orderId)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to upload");
                ViewBag.OrderId = orderId;
                return View();
            }

            try
            {
                var prescription = await _prescriptionService.UploadPrescriptionAsync(file, orderId);
                TempData["Success"] = "Prescription uploaded successfully. It will be reviewed by our pharmacist.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.OrderId = orderId;
                return View();
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> Verify()
        {
            var prescriptions = await _prescriptionService.GetPendingPrescriptionsAsync();
            return View(prescriptions);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> ProcessVerification(PrescriptionVerificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Verify");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool result = false;

            switch (model.Action)
            {
                case "Approve":
                    result = await _prescriptionService.VerifyPrescriptionAsync(
                        model.PrescriptionId, userId, true, model.Comments);
                    break;

                case "Reject":
                    result = await _prescriptionService.VerifyPrescriptionAsync(
                        model.PrescriptionId, userId, false, model.Comments);
                    break;

                case "RequestInfo":
                    result = await _prescriptionService.RequireAdditionalInfoAsync(
                        model.PrescriptionId, userId, model.RequestDetails);
                    break;
            }

            if (result)
            {
                TempData["Success"] = "Prescription processed successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to process prescription.";
            }

            return RedirectToAction("Verify");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var (fileContents, contentType, fileName) = await _prescriptionService.DownloadPrescriptionAsync(id);
                return File(fileContents, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Order");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> View(int id)
        {
            try
            {
                var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
                if (prescription == null)
                    return NotFound();

                return View(prescription);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Order");
            }
        }
    }
}
