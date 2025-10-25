namespace PharmaCom.Domain.ViewModels
{
    public class OrderItemViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal => Quantity * Price;
        public bool IsRxRequired { get; set; }
    }
}