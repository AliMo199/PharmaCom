using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string? GTIN { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsRxRequired { get; set; }
        public string Form { get; set; }
        public string? ImageURLString { get; set; }
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }
    }
}
