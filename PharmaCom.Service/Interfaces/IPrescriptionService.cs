using PharmaCom.Domain.Models;
using PharmaCom.Domain.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Service.Interfaces
{
    public interface IPrescriptionService
    {
        // Upload methods
        Task<Prescription> UploadPrescriptionAsync(IFormFile file, int orderId);
        Task<Prescription> SavePrescriptionFileAsync(IFormFile file, string userId);

        // Retrieval methods
        Task<Prescription> GetPrescriptionByIdAsync(int prescriptionId);
        Task<IEnumerable<Prescription>> GetPrescriptionsByOrderIdAsync(int orderId);
        Task<IEnumerable<PrescriptionViewModel>> GetPendingPrescriptionsAsync();

        // Verification methods
        Task<bool> VerifyPrescriptionAsync(int prescriptionId, string pharmacistId, bool isApproved, string comments);
        Task<bool> RequireAdditionalInfoAsync(int prescriptionId, string pharmacistId, string requestDetails);

        // Download method
        Task<(byte[] fileContents, string contentType, string fileName)> DownloadPrescriptionAsync(int prescriptionId);

        // Notification methods
        Task SendPrescriptionVerificationNotificationAsync(int prescriptionId, bool isApproved, string comments);
        Task<bool> HasPendingPrescriptionsAsync();
    }
}
