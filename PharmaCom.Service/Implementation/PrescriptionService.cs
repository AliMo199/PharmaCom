using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.Static;
using PharmaCom.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PharmaCom.Domain.ViewModels;

namespace PharmaCom.Service.Implementation
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public PrescriptionService(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _emailService = emailService;
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
                Comments = null
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

            // Create a prescription without an order (will be associated later)
            var prescription = new Prescription
            {
                FileUrl = $"/uploads/prescriptions/{uniqueFileName}",
                UploadDate = DateTime.UtcNow,
                Status = ST.Pending,
                UploadedByUserId = userId
            };

            await _unitOfWork.Prescription.AddAsync(prescription);
            _unitOfWork.Save();

            return prescription;
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
                // Get related order details
                var order = await _unitOfWork.Order.GetOrderWithDetailsAsync(prescription.OrderId);
                if (order == null) continue;

                // Create view model
                var viewModel = new PrescriptionViewModel
                {
                    PrescriptionId = prescription.Id,
                    OrderId = prescription.OrderId,
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

                result.Add(viewModel);
            }

            return result;
        }

        public async Task<bool> VerifyPrescriptionAsync(int prescriptionId, string pharmacistId, bool isApproved, string comments)
        {
            var prescription = await _unitOfWork.Prescription.GetPrescriptionWithOrderAsync(prescriptionId);
            if (prescription == null)
                return false;

            // Update prescription status
            prescription.Status = isApproved ? ST.Approved : ST.Rejected;
            prescription.Comments = comments;
            prescription.VerifiedByUserId = pharmacistId;
            prescription.VerificationDate = DateTime.UtcNow;

            _unitOfWork.Prescription.Update(prescription);

            // Update order status based on prescription approval
            if (prescription.Order != null)
            {
                prescription.Order.Status = isApproved ? ST.Approved : ST.Rejected;
                _unitOfWork.Order.Update(prescription.Order);
            }

            _unitOfWork.Save();

            // Send notification to customer
            await SendPrescriptionVerificationNotificationAsync(prescriptionId, isApproved, comments);

            return true;
        }

        public async Task<bool> RequireAdditionalInfoAsync(int prescriptionId, string pharmacistId, string requestDetails)
        {
            var prescription = await _unitOfWork.Prescription.GetPrescriptionWithOrderAsync(prescriptionId);
            if (prescription == null)
                return false;

            prescription.Status = ST.MoreInfoRequired;
            prescription.Comments = requestDetails;
            prescription.VerifiedByUserId = pharmacistId;
            prescription.VerificationDate = DateTime.UtcNow;

            _unitOfWork.Prescription.Update(prescription);
            _unitOfWork.Save();

            // Send notification to customer requesting more information
            await _emailService.SendEmailAsync(
                prescription.Order.ApplicationUser.Email,
                "Additional Information Required for Your Prescription",
                $"Please provide additional information for your prescription: {requestDetails}");

            return true;
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

        public async Task SendPrescriptionVerificationNotificationAsync(int prescriptionId, bool isApproved, string comments)
        {
            var prescription = await _unitOfWork.Prescription.GetPrescriptionWithOrderAsync(prescriptionId);
            if (prescription == null || prescription.Order?.ApplicationUser == null)
                return;

            var userEmail = prescription.Order.ApplicationUser.Email;
            if (string.IsNullOrEmpty(userEmail))
                return;

            var subject = isApproved
                ? "Your Prescription Has Been Approved"
                : "Your Prescription Could Not Be Approved";

            var message = isApproved
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
    }
}
