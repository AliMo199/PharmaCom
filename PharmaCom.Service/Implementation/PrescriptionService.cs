using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.Static;
using PharmaCom.Domain.ViewModels;
using PharmaCom.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Service.Implementation
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrescriptionService(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _emailService = emailService;
            _userManager = userManager;
        }

        public async Task<Prescription> UploadPrescriptionAsync(IFormFile file, int orderId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded");

            // Validate file type (only allow images and PDFs)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("File type not allowed. Please upload an image or PDF file.");

            // Get the order to ensure it exists
            var order = await _unitOfWork.Order.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Order with ID {orderId} not found");

            // Create directory if it doesn't exist
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "prescriptions");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate a unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Create prescription record
            var prescription = new Prescription
            {
                OrderId = orderId,
                FileUrl = $"/uploads/prescriptions/{uniqueFileName}",
                UploadDate = DateTime.UtcNow,
                Status = ST.Pending,
                Comments = null,
            };

            await _unitOfWork.Prescription.AddAsync(prescription);
            _unitOfWork.Save();

            // Update order with prescription ID and status
            order.PrescriptionId = prescription.Id;
            order.Status = ST.Pending; // Set order status to Pending
            _unitOfWork.Order.Update(order);
            _unitOfWork.Save();

            return prescription;
        }

        public async Task<Prescription> SavePrescriptionFileAsync(IFormFile file, string userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded");

            // Similar validation as above
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("File type not allowed. Please upload an image or PDF file.");

            // Validate file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size must be less than 5MB.");

            // Create directory if it doesn't exist
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "prescriptions");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate a unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // ✅ Create prescription without OrderId - it will be assigned later
            var prescription = new Prescription
            {
                FileUrl = $"/uploads/prescriptions/{uniqueFileName}",
                UploadDate = DateTime.UtcNow,
                Status = ST.Pending,
                UploadedByUserId = userId,
                OrderId = null // ✅ Explicitly set to null
            };

            await _unitOfWork.Prescription.AddAsync(prescription);
            _unitOfWork.Save();

            return prescription;
        }


        // ✅ Add helper method to get unassigned prescriptions
        public async Task<Prescription?> GetLatestApprovedPrescriptionForUserAsync(string userId)
        {
            var prescriptions = await _unitOfWork.Prescription.GetUnassignedPrescriptionsByUserIdAsync(userId);
            return prescriptions
                .Where(p => p.Status == ST.Approved && p.OrderId == null)
                .OrderByDescending(p => p.UploadDate)
                .FirstOrDefault();
        }

        public async Task<Prescription> GetPrescriptionByIdAsync(int prescriptionId)
        {
            return await _unitOfWork.Prescription.GetByIdAsync(prescriptionId);
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByOrderIdAsync(int orderId)
        {
            return await _unitOfWork.Prescription.GetPrescriptionsByOrderIdAsync(orderId);
        }

        public async Task<IEnumerable<PrescriptionViewModel>> GetPendingPrescriptionsAsync()
        {
            // Get all pending prescriptions
            var pendingPrescriptions = await _unitOfWork.Prescription.FindAsync(
                p => p.Status == ST.Pending);

            var result = new List<PrescriptionViewModel>();

            foreach (var prescription in pendingPrescriptions)
            {
                // ✅ Check if prescription has an associated order
                if (!prescription.OrderId.HasValue)
                {
                    var user = await _userManager.FindByIdAsync(prescription.UploadedByUserId);
                    var customerName = user?.UserName ?? user?.FullName ?? "Unknown";

                    var viewModel = new PrescriptionViewModel
                    {
                        PrescriptionId = prescription.Id,
                        OrderId = 0,
                        UploadDate = prescription.UploadDate,
                        Status = prescription.Status,
                        FileUrl = prescription.FileUrl,
                        Comments = prescription.Comments,
                        CustomerName = customerName, // ✅ Use actual name
                        OrderDate = prescription.UploadDate,
                        OrderTotal = 0m,
                        PrescriptionProducts = new List<PrescriptionProductViewModel>()
                    };

                    result.Add(viewModel);
                    continue;
                }

                // Get related order details
                var order = await _unitOfWork.Order.GetOrderWithDetailsAsync(prescription.OrderId.Value);
                if (order == null) continue;

                // Create view model with order details
                var viewModelWithOrder = new PrescriptionViewModel
                {
                    PrescriptionId = prescription.Id,
                    OrderId = prescription.OrderId.Value,
                    UploadDate = prescription.UploadDate,
                    Status = prescription.Status,
                    FileUrl = prescription.FileUrl,
                    Comments = prescription.Comments,

                    // Order details
                    CustomerName = order.ApplicationUser?.UserName ?? "Unknown",
                    OrderDate = order.OrderDate,
                    OrderTotal = order.TotalAmount,

                    // Get products that require prescription
                    PrescriptionProducts = order.OrderItems
                        .Where(item => item.Product != null && item.Product.IsRxRequired)
                        .Select(item => new PrescriptionProductViewModel
                        {
                            ProductId = item.ProductId,
                            Name = item.Product.Name,
                            Quantity = item.Quantity
                        }).ToList()
                };

                result.Add(viewModelWithOrder);
            }

            return result;
        }

        public async Task<bool> VerifyPrescriptionAsync(int prescriptionId, string pharmacistId, string Status, string comments)
        {
            try
            {
                // ✅ Get prescription with order
                var prescription = await _unitOfWork.Prescription.GetPrescriptionWithOrderAsync(prescriptionId);
                if (prescription == null)
                {
                    Console.WriteLine($"Prescription {prescriptionId} not found");
                    return false;
                }

                // ✅ Update prescription status
                prescription.Status = Status;
                prescription.Comments = comments;
                prescription.VerifiedByUserId = pharmacistId;
                prescription.VerificationDate = DateTime.UtcNow;

                _unitOfWork.Prescription.Update(prescription);

                // ✅ Update associated order status if exists
                if (prescription.OrderId.HasValue)
                {
                    var order = await _unitOfWork.Order.GetByIdAsync(prescription.OrderId.Value);

                    if (order != null)
                    {
                        if (Status==ST.Approved)
                        {
                            // Only update to Approved if order is in Pending or PaymentReceived status
                            if (order.Status == ST.Pending || order.Status == ST.PaymentReceived)
                            {
                                order.Status = ST.Approved;
                                prescription.Status = ST.Approved;
                                _unitOfWork.Order.Update(order);
                                Console.WriteLine($"Order {order.Id} status updated to Approved");
                            }
                        }
                        else
                        {
                            // If prescription is rejected, reject the order too
                            order.Status = ST.Rejected;
                            prescription.Status = ST.Rejected;
                            _unitOfWork.Order.Update(order);
                            Console.WriteLine($"Order {order.Id} status updated to Rejected");
                        }
                    }
                }

                // ✅ Save all changes
                var saveResult = _unitOfWork.Save();
                Console.WriteLine($"Save returned: {saveResult} changes");

                // Send notification to customer
                if (prescription.Order?.ApplicationUser?.Email != null)
                {
                    await SendPrescriptionVerificationNotificationAsync(prescriptionId, prescription.Status, comments);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyPrescriptionAsync: {ex.Message}\n{ex.StackTrace}");
                throw; // Re-throw to be caught by controller
            }
        }

        public async Task<bool> RequireAdditionalInfoAsync(int prescriptionId, string pharmacistId, string requestDetails)
        {
            try
            {
                var prescription = await _unitOfWork.Prescription.GetPrescriptionWithOrderAsync(prescriptionId);
                if (prescription == null)
                {
                    Console.WriteLine($"Prescription {prescriptionId} not found");
                    return false;
                }

                prescription.Status = ST.MoreInfoRequired;
                prescription.Comments = requestDetails;
                prescription.VerifiedByUserId = pharmacistId;
                prescription.VerificationDate = DateTime.UtcNow;

                _unitOfWork.Prescription.Update(prescription);

                // ✅ Update order status if exists
                if (prescription.OrderId.HasValue)
                {
                    var order = await _unitOfWork.Order.GetByIdAsync(prescription.OrderId.Value);
                    if (order != null)
                    {
                        order.Status = ST.MoreInfoRequired;
                        _unitOfWork.Order.Update(order);
                    }
                }

                var saveResult = _unitOfWork.Save();
                Console.WriteLine($"Save returned: {saveResult} changes");

                // Send notification to customer requesting more information
                if (prescription.Order?.ApplicationUser?.Email != null)
                {
                    await _emailService.SendEmailAsync(
                        prescription.Order.ApplicationUser.Email,
                        "Additional Information Required for Your Prescription",
                        $"Please provide additional information for your prescription: {requestDetails}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RequireAdditionalInfoAsync: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var prescriptions = await _unitOfWork.Prescription.FindAsync(
                p => p.UploadedByUserId == userId);

            return prescriptions.OrderByDescending(p => p.UploadDate);
        }

        

        public async Task<(byte[] fileContents, string contentType, string fileName)> DownloadPrescriptionAsync(int prescriptionId)
        {
            var prescription = await _unitOfWork.Prescription.GetByIdAsync(prescriptionId);
            if (prescription == null)
                throw new ArgumentException($"Prescription with ID {prescriptionId} not found");

            // Get file path from URL
            var fileUrl = prescription.FileUrl.TrimStart('/');
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, fileUrl);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Prescription file not found at {filePath}");

            // Determine content type
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            // Read file
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return (fileBytes, contentType, fileName);
        }

        public async Task SendPrescriptionVerificationNotificationAsync(int prescriptionId, string Status, string comments)
        {
            var prescription = await _unitOfWork.Prescription.GetPrescriptionWithOrderAsync(prescriptionId);
            if (prescription == null || prescription.Order?.ApplicationUser == null)
                return;

            var userEmail = prescription.Order.ApplicationUser.Email;
            if (string.IsNullOrEmpty(userEmail))
                return;

            var subject = Status==ST.Approved
                ? "Your Prescription Has Been Approved"
                : "Your Prescription Could Not Be Approved";

            var message = Status==ST.Approved
                ? $"Good news! Your prescription has been approved and your order is now being processed. Order #: {prescription.OrderId}"
                : $"Unfortunately, your prescription could not be approved. Reason: {comments}. Order #: {prescription.OrderId}";

            await _emailService.SendEmailAsync(userEmail, subject, message);
        }

        public async Task<bool> HasPendingPrescriptionsAsync()
        {
            var pendingPrescriptions = await _unitOfWork.Prescription.FindAsync(
                p => p.Status == ST.Pending);

            return pendingPrescriptions.Any();
        }

        /// <summary>
        /// Gets the latest prescription (pending or approved) that hasn't been assigned to an order
        /// </summary>
        public async Task<Prescription?> GetLatestAvailablePrescriptionForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var prescriptions = await _unitOfWork.Prescription.GetUnassignedPrescriptionsByUserIdAsync(userId);
            return prescriptions
                .Where(p => (p.Status == ST.Pending || p.Status == ST.Approved) && p.OrderId == null)
                .OrderByDescending(p => p.UploadDate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Checks if user has any unassigned prescription (pending or approved)
        /// </summary>
        public async Task<bool> UserHasAvailablePrescriptionAsync(string userId)
        {
            var prescription = await GetLatestAvailablePrescriptionForUserAsync(userId);
            return prescription != null;
        }
    }
}
