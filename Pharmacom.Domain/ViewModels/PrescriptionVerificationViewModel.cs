using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class PrescriptionVerificationViewModel
    {
        [Required]
        public int PrescriptionId { get; set; }

        public bool IsApproved { get; set; }

        public string? Comments { get; set; }

        public string? RequestDetails { get; set; }

        [Required]
        public string Action { get; set; } // Approve, Reject, RequestInfo
    }
}