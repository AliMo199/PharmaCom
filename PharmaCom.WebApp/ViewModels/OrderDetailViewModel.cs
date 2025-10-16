using System.Collections.Generic;

namespace PharmaCom.WebApp.ViewModels
{
    public class OrderDetailViewModel
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public AddressViewModel ShippingAddress { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }

        public OrderDetailViewModel()
        {
            OrderItems = new List<OrderItemViewModel>();
            ShippingAddress = new AddressViewModel();
        }
    }
}