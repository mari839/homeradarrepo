using System.Text.Json.Serialization;

namespace HomeRadar.Services.SsGe.Models;

public class SsGeSearchRequest
{
    [JsonPropertyName("realEstateType")]
    public int? RealEstateType { get; set; }

    [JsonPropertyName("realEstateDealType")]
    public int? RealEstateDealType { get; set; }

    [JsonPropertyName("cityIdList")]
    public List<int>? CityIdList { get; set; }

    [JsonPropertyName("municipalityId")]
    public int? MunicipalityId { get; set; }

    [JsonPropertyName("subdistrictIds")]
    public List<int>? SubdistrictIds { get; set; }

    [JsonPropertyName("streetIds")]
    public List<int>? StreetIds { get; set; }

    [JsonPropertyName("subwayStation")]
    public List<string>? SubwayStation { get; set; }

    [JsonPropertyName("subwayStationDistance")]
    public int? SubwayStationDistance { get; set; }

    [JsonPropertyName("currencyId")]
    public int? CurrencyId { get; set; }

    [JsonPropertyName("priceType")]
    public int? PriceType { get; set; }

    [JsonPropertyName("priceFrom")]
    public int? PriceFrom { get; set; }

    [JsonPropertyName("priceTo")]
    public int? PriceTo { get; set; }

    [JsonPropertyName("areaFrom")]
    public int? AreaFrom { get; set; }

    [JsonPropertyName("areaTo")]
    public int? AreaTo { get; set; }

    [JsonPropertyName("rooms")]
    public List<int>? Rooms { get; set; }

    [JsonPropertyName("bedroomsCount")]
    public List<int>? BedroomsCount { get; set; }

    [JsonPropertyName("realEstateStatuses")]
    public List<int>? RealEstateStatuses { get; set; }

    [JsonPropertyName("commercialTypes")]
    public List<int>? CommercialTypes { get; set; }

    [JsonPropertyName("offerType")]
    public List<int>? OfferType { get; set; }

    [JsonPropertyName("advancedSearch")]
    public SsGeAdvancedSearch? AdvancedSearch { get; set; }

    [JsonPropertyName("order")]
    public int? Order { get; set; }

    [JsonPropertyName("searchString")]
    public string? SearchString { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 16;

    [JsonPropertyName("considerSimilarity")]
    public bool? ConsiderSimilarity { get; set; }
}

public class SsGeAdvancedSearch
{
    [JsonPropertyName("balcony")]
    public bool? Balcony { get; set; }

    [JsonPropertyName("elevator")]
    public bool? Elevator { get; set; }

    [JsonPropertyName("furniture")]
    public bool? Furniture { get; set; }

    [JsonPropertyName("heating")]
    public bool? Heating { get; set; }

    [JsonPropertyName("hotWater")]
    public bool? HotWater { get; set; }

    [JsonPropertyName("storageRoom")]
    public bool? StorageRoom { get; set; }

    [JsonPropertyName("parkingType")]
    public int? ParkingType { get; set; }

    [JsonPropertyName("airConditioning")]
    public bool? AirConditioning { get; set; }
}
