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
        public int ApplicationUserId { get; set; }
        public int AddressId { get; set; }
        public int PrescriptionId { get; set; }
        public int SessionId { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }
        public virtual Address? Address { get; set; }
        public List<Prescription>? Prescription { get; set; }
    }
}
