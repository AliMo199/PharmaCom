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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadPrescriptionForCheckout(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file selected" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ✅ Create temporary prescription (will be linked to order in next step)
                var prescription = await _prescriptionService.SavePrescriptionFileAsync(file, userId);

                return Json(new
                {
                    success = true,
                    prescriptionId = prescription.Id,
                    message = "Prescription uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get user's prescriptions (for cart page display)
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPrescriptions()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Get prescriptions uploaded by this user
                var prescriptions = await _prescriptionService.GetPrescriptionsByUserIdAsync(userId);

                var prescriptionData = prescriptions.Select(p => new
                {
                    id = p.Id,
                    fileUrl = p.FileUrl,
                    uploadDate = p.UploadDate,
                    status = p.Status,
                    comments = p.Comments
                }).ToList();

                return Json(new { success = true, prescriptions = prescriptionData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
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
        //[Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> Verify()
        {
            var prescriptions = await _prescriptionService.GetPendingPrescriptionsAsync();
            return View(prescriptions);
        }

        [HttpPost]
        //[Authorize(Roles = "Admin,Pharmacist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessVerification(PrescriptionVerificationViewModel model)
        {
            try
            {
                // ✅ Add validation logging
                if (model.PrescriptionId <= 0)
                {
                    TempData["Error"] = "Invalid prescription ID";
                    return RedirectToAction("Verify");
                }

                if (string.IsNullOrWhiteSpace(model.Action))
                {
                    TempData["Error"] = "No action specified";
                    return RedirectToAction("Verify");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool result = false;

                switch (model.Action)
                {
                    case "Approve":
                        result = await _prescriptionService.VerifyPrescriptionAsync(
                            model.PrescriptionId, userId, true, model.Comments ?? "Approved by pharmacist");

                        if (result)
                        {
                            TempData["Success"] = "Prescription approved successfully!";
                        }
                        else
                        {
                            TempData["Error"] = "Failed to approve prescription";
                        }
                        break;

                    case "Reject":
                        if (string.IsNullOrWhiteSpace(model.Comments))
                        {
                            TempData["Error"] = "Please provide a reason for rejection";
                            return RedirectToAction("Verify");
                        }

                        result = await _prescriptionService.VerifyPrescriptionAsync(
                            model.PrescriptionId, userId, false, model.Comments);

                        if (result)
                        {
                            TempData["Success"] = "Prescription rejected";
                        }
                        else
                        {
                            TempData["Error"] = "Failed to reject prescription";
                        }
                        break;

                    case "RequestInfo":
                        if (string.IsNullOrWhiteSpace(model.RequestDetails))
                        {
                            TempData["Error"] = "Please specify what information is needed";
                            return RedirectToAction("Verify");
                        }

                        result = await _prescriptionService.RequireAdditionalInfoAsync(
                            model.PrescriptionId, userId, model.RequestDetails);

                        if (result)
                        {
                            TempData["Success"] = "Information request sent to customer";
                        }
                        else
                        {
                            TempData["Error"] = "Failed to send information request";
                        }
                        break;

                    default:
                        TempData["Error"] = "Invalid action: " + model.Action;
                        break;
                }
            }
            catch (Exception ex)
            {
                // ✅ Add detailed error logging
                TempData["Error"] = $"Error processing prescription: {ex.Message}";
                // TODO: Add proper logging here
                Console.WriteLine($"Error in ProcessVerification: {ex.Message}\n{ex.StackTrace}");
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