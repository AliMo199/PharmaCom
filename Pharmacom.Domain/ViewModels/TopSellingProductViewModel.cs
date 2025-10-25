using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class TopSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
        public string Category { get; set; }
    }
}
