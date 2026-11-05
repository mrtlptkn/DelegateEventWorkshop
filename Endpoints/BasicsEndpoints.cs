using DelegateEventWorkshop.Basics.Delegates;
using DelegateEventWorkshop.Basics.Delegates.Lab;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Reflection;


namespace DelegateEventWorkshop.Endpoints;

// Bölüm 1'deki dört konunun Minimal API endpoint tanımları.
// MapGroup: ortak prefix ve ortak etiketi (Swagger'da gruplama) tek yerde tanımlar.
public static class BasicsEndpoints
{
    // parametre olarak delegate alan bir metot örneği.
    static int Calculate(int a, int b, MathOp operation) => operation(a, b);

    // 1.1 Delegate
    public static IEndpointRouteBuilder MapDelegateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delegates").WithTags("1.1 Delegate");

        // Strateji değiştirme: işlem adına göre doğru delegate seçilir.
        group.MapGet("/demo", () =>
        {

            // ADIM 1: Delegate nedir?
            // Delegate, bir metodu değişken gibi taşımamızı sağlayan tiptir.
            // =====================================================================
            Console.WriteLine("=== 1. Delegate ===");

            MathOp op = MathOps.Add;          // metodu delegate'e atadık
            Console.WriteLine(op(3, 4));      // 7

            op = MathOps.Multiply;            // aynı değişkene başka metot atadık
            Console.WriteLine(op(3, 4));      // 12

            // Delegate'i parametre olarak geçmek: "ne yapılacağını" çağıran belirler.
            Console.WriteLine(Calculate(10, 2, MathOps.Add));       // 12
            Console.WriteLine(Calculate(10, 2, MathOps.Multiply));  // 20


            // =====================================================================
            // ADIM 2: Multicast delegate
            // Bir delegate birden fazla metoda işaret edebilir (+= ile eklenir, -= ile çıkarılır).
            // =====================================================================
            Console.WriteLine("\n=== 2. Multicast delegate ===");

            Action<string> notify = msg => Console.WriteLine($"Email: {msg}");
            notify += msg => Console.WriteLine($"SMS: {msg}");
            notify += msg => Console.WriteLine($"Log: {msg}");

            notify("Order created!");  // 3 handler çalışır


            // =====================================================================
            // ADIM 3: Action, Func, Predicate
            // Her seferinde delegate tanımlamak yerine hazır tipleri kullanırız.
            //   Action<T>        -> değer döndürmez
            //   Func<T, TResult> -> değer döndürür (son tip parametresi dönüş tipidir)
            //   Predicate<T>     -> bool döndürür
            // =====================================================================
            Console.WriteLine("\n=== 3. Action / Func / Predicate ===");

            Action<string> print = text => Console.WriteLine(text);
            print("Hello from Action");

            Func<int, int, int> add = (a, b) => a + b;
            Console.WriteLine(add(2, 3));   // 5

            Predicate<int> isEven = n => n % 2 == 0;
            Console.WriteLine(isEven(4));   // True
            Console.WriteLine(isEven(5));   // False

            // LINQ'nun arkasında da Func delegate'leri vardır:
            var numbers = new List<int> { 1, 2, 3, 4, 5, 6 };
            var evenSquares = numbers.Where(n => n % 2 == 0).Select(n => n * n);
            Console.WriteLine(string.Join(", ", evenSquares));   // 4, 16, 36


            // =====================================================================
            // ADIM 4: Event
            // Event, delegate'in "korumalı" halidir: dışarıdan sadece += ve -= yapılabilir.
            // Yayıncı (publisher) olayı tetikler, aboneler (subscriber) dinler.
            // =====================================================================
            Console.WriteLine("\n=== 4. Event ===");

            var downloader = new Downloader();
            downloader.Completed += fileName => Console.WriteLine($"Subscriber 1: {fileName} downloaded");
            downloader.Completed += fileName => Console.WriteLine($"Subscriber 2: notifying user about {fileName}");

            downloader.Start("report.pdf");

            // Aşağıdaki satır DERLENMEZ, event'i dışarıdan sıfırlayamayız. Yorumu kaldırıp deneyin:
            // downloader.Completed = null;



            // =====================================================================
            // ADIM 5: Event ile delegate alanı farkı
            // Aynı iş, ama delegate alanı herkese açık olursa neler olur?
            // =====================================================================
            Console.WriteLine("\n=== 5. Event vs delegate field ===");

            var risky = new RiskyPublisher();
            risky.OnMessage += m => Console.WriteLine($"A got: {m}");
            risky.OnMessage += m => Console.WriteLine($"B got: {m}");

            risky.OnMessage = m => Console.WriteLine($"C got: {m}");   // A ve B sessizce silindi!
            risky.Publish("hello");                                      // sadece C çalışır


            // =====================================================================
            // ADIM 6: Standart event kalıbı (EventHandler<T> + EventArgs)
            // .NET dünyasındaki yaygın imza: (object? sender, TEventArgs e)
            // =====================================================================
            Console.WriteLine("\n=== 6. EventHandler<T> ===");

            var shop = new Basics.Delegates.Shop();
            shop.OrderPlaced += (sender, e) => Console.WriteLine($"Order #{e.OrderId} placed by {e.Customer}");
            shop.PlaceOrder("Ayşe");
            shop.PlaceOrder("Mehmet");


            // =====================================================================
            // ADIM 7: Reflection ile Delegate (isteğe bağlı kısa bölüm)
            // Metot adı elimizde string olarak varsa, onu delegate'e çevirebiliriz.
            // =====================================================================
            Console.WriteLine("\n=== 7. Reflection + Delegate ===");

            MethodInfo method = typeof(MathOps).GetMethod("Add")!;

            // Yavaş yol: her çağrıda reflection
            object? slow = method.Invoke(null, new object[] { 3, 4 });
            Console.WriteLine($"Invoke: {slow}");

            // Hızlı yol: bir kez delegate'e çevir, sonra normal delegate gibi kullan
            var fast = (MathOp)Delegate.CreateDelegate(typeof(MathOp), method);
            Console.WriteLine($"CreateDelegate: {fast(3, 4)}");


        });

        // Delegate, Action ve Func ile “ne yapılacağını” dışarıdan veriyoruz. Event ile “bir şey oldu” diye haber veriyoruz. İkisinde de ana sınıf değişmeden davranış değişiyor.
        // Shop sınıfı tüm demo boyunca hiç değişmiyor, değişen sadece dışarıdan verdiğimiz parçalar.
        // Delegate, Action ve Func “tek bir parça davranış verme” aracıdır.Event ise “çok sayıda dinleyiciye haber verme” aracıdır.
        group.MapGet("/lab", () =>
        {
            // Action: Shop'un nasıl loglanacağını dışarıdan biz belirliyoruz.
            Action<string> logger = message => Console.WriteLine($"  [LOG] {message}");

            var orderService = new OrderService(logger);

            // Func: doğrulama kurallarını koddan değil, dışarıdan (lambda ile) veriyoruz.
            orderService.AddRule("Quantity must be positive", order => order.Quantity > 0);
            orderService.AddRule("Total must not exceed 100000", order => order.Quantity * order.UnitPrice <= 10000m);


            // Event: abonelikler. Shop kimlerin dinlediğini bilmez.
            var email = new EmailService();
            orderService.OrderPlaced += email.OnOrderPlaced;   // metot ile abone

            // Abonelikten çıkmak için lambda'yı bir değişkende tutuyoruz.
            EventHandler<Basics.Delegates.Lab.OrderPlacedEventArgs> smsHandler =
                (sender, e) => Console.WriteLine($"  [SMS] Order #{e.OrderId} confirmed");
            orderService.OrderPlaced += smsHandler;            // lambda ile abone

            orderService.OrderRejected += reason => Console.WriteLine($"  [REJECTED] {reason}");


            // ---- Senaryo ----

            Console.WriteLine("--- 1. Normal order, no discount ---");
            orderService.Discount = Discounts.NoDiscount;      // delegate'e metot atadık
            orderService.PlaceOrder(new Order("Alice", "Mouse", 2, 450m));

            Console.WriteLine("\n--- 2. Invalid order (quantity 0) ---");
            orderService.PlaceOrder(new Order("Bob", "Keyboard", 0, 1200m));

            Console.WriteLine("\n--- 3. Strategy changed at runtime: 10% off ---");
            orderService.Discount = Discounts.TenPercentOff;   // aynı delegate, başka metot
            orderService.PlaceOrder(new Order("Carol", "Keyboard", 1, 1200m));

            Console.WriteLine("\n--- 4. Strategy as a lambda: 50% off ---");
            orderService.Discount = Discounts.HalfPrice; 
            orderService.PlaceOrder(new Order("Dave", "Laptop", 1, 25000m));


            Console.WriteLine("\n--- 4. Strategy as a lambda: 30% off ---");
            orderService.Discount = total => total * 0.30m;    // metot yazmadan, lambda ile strateji
            orderService.PlaceOrder(new Order("Dave", "Laptop", 1, 25000m));

            Console.WriteLine("\n--- 5. Order rejected by the total rule ---");
            orderService.PlaceOrder(new Order("Eve", "Laptop", 10, 20000m));

            Console.WriteLine("\n--- 6. SMS unsubscribed (-=) ---");
            orderService.OrderPlaced -= smsHandler;
            orderService.PlaceOrder(new Order("Frank", "Notebook", 10, 85m));

            // ---- Özet: LINQ'daki Count ve Sum da Func delegate'leri ile çalışır ----

            Console.WriteLine("\n--- Summary ---");
            Func<(Order Order, decimal Total), bool> isBigOrder = x => x.Total >= 500m;

            Console.WriteLine($"Orders placed: {orderService.PlacedOrders.Count}");
            Console.WriteLine($"Revenue: {orderService.PlacedOrders.Sum(x => x.Total)}");
            Console.WriteLine($"Big orders (>= 500): {orderService.PlacedOrders.Count(isBigOrder)}");


        });


        return app;
    }

  
}
