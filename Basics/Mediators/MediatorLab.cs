using DelegateEventWorkshop.Basics.Delegates.Lab;
using MediatR;

namespace DelegateEventWorkshop.Basics.Mediators
{
    // REQUEST (Command): "bir sipariş ver" isteği. IRequest<int>: cevap olarak int (sipariş no) döner.
    // Bir request'in TAM OLARAK BİR handler'ı olur.
    record PlaceOrderCommand(Order Order) : IRequest<int>;


    // NOTIFICATION: "bir sipariş verildi" haberi. Önceki demodaki event'in karşılığı.
    // Bir notification'ın SIFIR veya DAHA FAZLA handler'ı olabilir.
    record OrderPlacedNotification(int OrderId, string Customer, decimal Total) : INotification;

    class DiscountSettings
    {
        public DiscountStrategy Discount { get; set; } = Discounts.NoDiscount;
    }

    class OrderRules
    {
        public List<(string Name, Func<Order, bool> Check)> All { get; } = new();

        public void Add(string name, Func<Order, bool> check) => All.Add((name, check));
    }

    class OrderStore
    {
        public List<(Order Order, decimal Total)> PlacedOrders { get; } = new();

        // Sipariş numarasını döndürür.
        public int Add(Order order, decimal total)
        {
            PlacedOrders.Add((order, total));
            return PlacedOrders.Count;
        }
    }

    // HANDLER (request): önceki demodaki Shop.PlaceOrder metodunun içi artık burada.
    class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, int>
    {
        private readonly DiscountSettings _settings;
        private readonly OrderStore _store;
        private readonly IPublisher _publisher;
        private readonly Action<string> _log;

        // Bağımlılıklar constructor'dan gelir (Dependency Injection).
        public PlaceOrderHandler(DiscountSettings settings, OrderStore store, IPublisher publisher, Action<string> log)
        {
            _settings = settings;
            _store = store;
            _publisher = publisher;
            _log = log;
        }

        public async Task<int> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
        {
            var order = request.Order;

            // Delegate burada hâlâ kullanılıyor: indirim mantığı dışarıdan verilen metottur.
            var total = _settings.Discount(order.Quantity * order.UnitPrice);
            var orderId = _store.Add(order, total);

            _log($"Order #{orderId} total after discount: {total}");

            // ADIM 2: Publish -> OrderPlacedNotification için kayıtlı TÜM handler'lar çalışır.
            // Önceki demodaki "OrderPlaced?.Invoke(...)" satırının karşılığı.
            await _publisher.Publish(new OrderPlacedNotification(orderId, order.Customer, total), cancellationToken);

            return orderId;
        }
    }

    // HANDLER'LAR (notification): önceki demodaki aboneler. "+=" yok, sınıfın var olması yeterli.
    // Not: Aynı notification'ı dinleyen handler'ların çalışma sırasına güvenmeyin.
    class EmailHandler : INotificationHandler<OrderPlacedNotification>
    {
        public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  [EMAIL] Confirmation sent to {notification.Customer} (total: {notification.Total})");
            return Task.CompletedTask;
        }
    }

    class SmsHandler : INotificationHandler<OrderPlacedNotification>
    {
        public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  [SMS] Order #{notification.OrderId} confirmed");
            return Task.CompletedTask;
        }
    }

    // ADIM 3: PIPELINE BEHAVIOR
    // Her request, handler'a ulaşmadan önce ve sonra bu katmanlardan geçer.
    // DİKKAT: Behavior'lar sadece Send ile gönderilen request'lere uygulanır, Publish'e uygulanmaz.
    // 'next' bir DELEGATE'tir: çağrılınca zincirdeki sonraki behavior'ı ya da handler'ı çalıştırır.

    // Önceki demodaki Action<string> logger'ın yerine geçer: handler'a dokunmadan loglama.
    class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Action<string> _log;

        public LoggingBehavior(Action<string> log) => _log = log;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            _log($"-> {requestName}");

            try
            {
                var response = await next();   // sıradaki katmana geç
                _log($"<- {requestName} done");
                return response;
            }
            catch (Exception ex)
            {
                _log($"!! {requestName} failed: {ex.Message}");
                throw;
            }
        }
    }

    class OrderRejectedException : Exception
    {
        public OrderRejectedException(string message) : base(message)
        {
        }
    }


    // Önceki demodaki Func<Order,bool> kurallarını çalıştırır; kural bozulursa handler HİÇ çalışmaz.
    class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly OrderRules _rules;

        public ValidationBehavior(OrderRules rules) => _rules = rules;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is PlaceOrderCommand command)
            {
                foreach (var (name, check) in _rules.All)
                {
                    if (!check(command.Order))
                    {
                        
                        Console.WriteLine($"Order rejected: {command.Order.Customer} - {name}");
                        throw new OrderRejectedException($"{command.Order.Customer}: {name}");
                    }
                }
            }

            return await next();
        }
    }


}
