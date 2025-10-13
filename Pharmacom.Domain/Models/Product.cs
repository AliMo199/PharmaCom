using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Brand { get; set; }
        public string? GTIN { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public bool IsRxRequired { get; set; }
        [Required]
        public string Form { get; set; }
        public string? ImageURLString { get; set; }
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }
    }
}
