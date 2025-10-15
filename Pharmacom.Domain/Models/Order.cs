using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public int AddressId { get; set; }
        public int? PrescriptionId { get; set; }
        public string SessionId { get; set; }
        public string PaymentIntentId { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }
        public virtual Address? Address { get; set; }
        public List<Prescription>? Prescription { get; set; }
    }
}
