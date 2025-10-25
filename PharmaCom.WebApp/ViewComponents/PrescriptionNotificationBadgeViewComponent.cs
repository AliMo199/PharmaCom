using Microsoft.AspNetCore.Mvc;
using PharmaCom.Service.Interfaces;

namespace PharmaCom.WebApp.ViewComponents
{
    public class PrescriptionNotificationBadgeViewComponent : ViewComponent
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionNotificationBadgeViewComponent(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var hasPendingPrescriptions = await _prescriptionService.HasPendingPrescriptionsAsync();
            return View(hasPendingPrescriptions);
        }
    }
}
