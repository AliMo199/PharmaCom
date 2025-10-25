using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class PrescriptionVerificationViewModel
    {
        public int PrescriptionId { get; set; }
        public bool IsApproved { get; set; }
        public string Comments { get; set; }
        public string RequestDetails { get; set; }
        public string Action { get; set; } // Approve, Reject, RequestInfo
    }
}
