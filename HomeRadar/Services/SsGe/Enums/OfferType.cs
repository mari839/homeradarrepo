namespace HomeRadar.Services.SsGe.Enums;

public enum OfferType
{
    TopOffer = 1,       // შეღავათი ფასი - Best/Discounted price (green)
    Cheap = 2,          // საბაზრო ფასი - Market/Cheap price (light green)
    Medium = 3,         // საშუალო ფასი - Average price (yellow)
    Expensive = 4,      // ძვირი ფასი - Expensive (orange)
    VeryExpensive = 5   // ძალიან ძვირი - Very expensive (red)
}
