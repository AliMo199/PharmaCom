using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Models
{
    public class Prescription
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string FileUrl { get; set; }
        public virtual Order? Order { get; set; }
    }
}
