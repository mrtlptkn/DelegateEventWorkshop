using DelegateEventWorkshop.Basics.Delegates;
using DelegateEventWorkshop.Basics.Delegates.Lab;
using DelegateEventWorkshop.Basics.Mediators;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Reflection;


namespace DelegateEventWorkshop.Endpoints;

// Bölüm 1'deki dört konunun Minimal API endpoint tanımları.
// MapGroup: ortak prefix ve ortak etiketi (Swagger'da gruplama) tek yerde tanımlar.
public static class MediatREndpoints
{

    public static IEndpointRouteBuilder MapMediatREndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mediatr").WithTags("1.1 Delegate");

        // Strateji değiştirme: işlem adına göre doğru delegate seçilir.
        group.MapPost("/demo", async (OrderRules rules, DiscountSettings settings, ISender sender) =>
        {

            rules.Add("Quantity must be positive", order => order.Quantity > 0);
            rules.Add("Total must not exceed 10000", order => order.Quantity * order.UnitPrice <= 100m);


            //// sample01
            //settings.Discount = Discounts.HalfPrice;
            //var command = new PlaceOrderCommand(new Order("Bob", "Gadget", 0, 100m));
            //var response = await sender.Send(command);

            //// sample02
            //settings.Discount = Discounts.TenPercentOff;
            //var command2 = new PlaceOrderCommand(new Order("Alice", "Widget", 3, 150m));
            //var response2 = await sender.Send(command2);


            // sample03
            settings.Discount = Discounts.NoDiscount;
            var command3 = new PlaceOrderCommand(new Order("Charlie", "Thingamajig", 1, 50m));
            var response3 = await sender.Send(command3);



            return Results.Ok(response3);


        });


        return app;
    }


}
