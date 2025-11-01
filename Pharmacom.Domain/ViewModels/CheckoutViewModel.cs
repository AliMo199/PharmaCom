using PharmaCom.Domain.Models;


namespace PharmaCom.Domain.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public AddressViewModel ShippingAddress { get; set; } = new AddressViewModel();
        public decimal TotalAmount { get; set; }
        public bool RequiresPrescription { get; set; }
        public int? PrescriptionId { get; set; }
    }
}