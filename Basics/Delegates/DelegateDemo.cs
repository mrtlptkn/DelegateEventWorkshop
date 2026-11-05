namespace DelegateEventWorkshop.Basics.Delegates;

public delegate int MathOp(int a, int b);

public static class MathOps
{
    public static int Add(int a, int b) => a + b;

    public static int Multiply(int a, int b) => a * b;
}

public class Downloader
{
    // Action<string> tipinde bir event: dışarıdan sadece += / -=
    public event Action<string>? Completed;

    public void Start(string fileName)
    {
        Console.WriteLine($"Downloading {fileName}...");

        // '?.' abone yoksa hata oluşmasını engeller.
        Completed?.Invoke(fileName);
    }
}

public class RiskyPublisher
{
    // 'event' yok: dışarıdan atama (=) ve doğrudan çağırma mümkün, tehlikeli.
    public Action<string>? OnMessage;

    public void Publish(string message) => OnMessage?.Invoke(message);
}

public class OrderPlacedEventArgs : EventArgs
{
    public int OrderId { get; init; }

    public string Customer { get; init; } = "";
}

public class Shop
{
    private int _nextId = 1;

    public event EventHandler<OrderPlacedEventArgs>? OrderPlaced;

    public void PlaceOrder(string customer)
    {
        OrderPlaced?.Invoke(this, new OrderPlacedEventArgs { OrderId = _nextId++, Customer = customer });
    }
}
