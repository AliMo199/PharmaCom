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
        Task<Prescription> UploadPrescriptionAsync(IFormFile file, int orderId);
        Task<Prescription> SavePrescriptionFileAsync(IFormFile file, string userId);
        Task<Prescription> GetPrescriptionByIdAsync(int prescriptionId);
        Task<IEnumerable<Prescription>> GetPrescriptionsByOrderIdAsync(int orderId);
        Task<IEnumerable<Prescription>> GetPrescriptionsByUserIdAsync(string userId);
        Task<Prescription?> GetLatestApprovedPrescriptionForUserAsync(string userId);
        Task<Prescription?> GetLatestAvailablePrescriptionForUserAsync(string userId); // ✅ NEW
        Task<bool> UserHasAvailablePrescriptionAsync(string userId); // ✅ NEW
        Task<IEnumerable<PrescriptionViewModel>> GetPendingPrescriptionsAsync();
        Task<bool> VerifyPrescriptionAsync(int prescriptionId, string pharmacistId, string Status, string comments);
        Task<bool> RequireAdditionalInfoAsync(int prescriptionId, string pharmacistId, string requestDetails);
        Task<(byte[] fileContents, string contentType, string fileName)> DownloadPrescriptionAsync(int prescriptionId);
        Task SendPrescriptionVerificationNotificationAsync(int prescriptionId, string Status, string comments);
        Task<bool> HasPendingPrescriptionsAsync();
    }
}
