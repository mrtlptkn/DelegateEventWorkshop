namespace DelegateEventWorkshop.Basics.Delegates.Lab
{
    // Custom delegate: toplam tutarı alıp indirimli tutarı döndüren metotları temsil eder.
    public delegate decimal DiscountStrategy(decimal total);

    public record Order(string Customer, string Product, int Quantity, decimal UnitPrice);

    public static class Discounts
    {
        // İmzaları DiscountStrategy ile uyumlu metotlar.
        public static decimal NoDiscount(decimal total) => total;

        public static decimal HalfPrice(decimal total) => total * 0.50m;
      
        public static decimal TenPercentOff(decimal total) => total * 0.90m;
    }

    // Sipariş bilgilerini taşırız
    public class OrderPlacedEventArgs : EventArgs
    {
        public int OrderId { get; init; }

        public string Customer { get; init; } = "";

        public decimal Total { get; init; }
    }

    public class EmailService
    {
        // İmza EventHandler<OrderPlacedEventArgs> ile uyumlu olmalı.
        public void OnOrderPlaced(object? sender, OrderPlacedEventArgs e)
        {
            Console.WriteLine($"  [EMAIL] Confirmation sent to {e.Customer} (total: {e.Total})");
        }
    }

    // Sipariş 
    public class OrderService
    {
        private readonly Action<string> _log;
        private readonly List<(string Name, Func<Order, bool> Check)> _rules = new();
        private int _nextId = 1;

        public OrderService(Action<string> log) => _log = log;

        // Delegate özelliği: indirim mantığı çalışma zamanında değiştirilebilir.
        public DiscountStrategy Discount { get; set; } = Discounts.NoDiscount;

        public List<(Order Order, decimal Total)> PlacedOrders { get; } = new();

        // Event (standart kalıp): EventHandler<TEventArgs>
        public event EventHandler<OrderPlacedEventArgs>? OrderPlaced;

        // Event (basit kalıp): Action<string>
        public event Action<string>? OrderRejected;

        public void AddRule(string name, Func<Order, bool> check) => _rules.Add((name, check));

        public void PlaceOrder(Order order)
        {
            _log($"Processing order for {order.Customer}");

            foreach (var (name, check) in _rules)
            {
                if (!check(order))
                {
                    OrderRejected?.Invoke($"{order.Customer}: {name}");
                    return;
                }
            }

            var total = Discount(order.Quantity * order.UnitPrice);
            var orderId = _nextId++;
            PlacedOrders.Add((order, total));

            _log($"Order #{orderId} total after discount: {total}");

            // Multicast: OrderPlaced'e bağlı tüm aboneler sırayla çalışır.
            OrderPlaced?.Invoke(this, new OrderPlacedEventArgs
            {
                OrderId = orderId,
                Customer = order.Customer,
                Total = total
            });
        }
    }

}
