using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string Line1 { get; set; }
        public string City { get; set; }
        public string Governorate { get; set; }
        public bool IsDefault { get; set; } = false;
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}
