using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class PrescriptionViewModel
    {
        public int PrescriptionId { get; set; }
        public int OrderId { get; set; }
        public DateTime UploadDate { get; set; }
        public string Status { get; set; }
        public string FileUrl { get; set; }
        public string? Comments { get; set; }

        // Order and customer information
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal OrderTotal { get; set; }
        public List<PrescriptionProductViewModel> PrescriptionProducts { get; set; } = new List<PrescriptionProductViewModel>();
    }
}
