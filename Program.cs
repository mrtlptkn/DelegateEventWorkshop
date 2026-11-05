using DelegateEventWorkshop.Basics.Mediators;
using DelegateEventWorkshop.Endpoints;
var builder = WebApplication.CreateBuilder(args);



// Swagger: eğitim sırasında endpoint'leri tarayıcıdan denemek için.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




// Bölüm 3: MediatR ile in-memory event-driven sürüm.
// Action<string>'i de DI'a kaydediyoruz; behavior ve handler'lar bunu constructor'dan alır.
Action<string> logger = message => Console.WriteLine($"  [LOG] {message}");
builder.Services.AddSingleton(logger);

builder.Services.AddSingleton<DiscountSettings>();   // indirim delegate'ini tutar
builder.Services.AddSingleton<OrderRules>();         // Func doğrulama kurallarını tutar
builder.Services.AddSingleton<OrderStore>();         // siparişleri bellekte tutar

builder.Services.AddMediatR(cfg =>
{
    // Bu assembly'deki tüm handler sınıflarını otomatik bulup kaydeder.
    cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>();

    // Behavior'lar kayıt sırasıyla iç içe çalışır: önce Logging (dış), sonra Validation, en içte handler.
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Kök adrese gelen istekleri Swagger arayüzüne yönlendir.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Bölüm 1: Event Delegates
app.MapDelegateEndpoints();
// Bölüm 2: MediaTR
app.MapMediatREndpoints();

app.Run();
