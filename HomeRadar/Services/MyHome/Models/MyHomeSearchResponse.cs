using System.Text.Json.Serialization;

namespace HomeRadar.Services.MyHome.Models;

public class MyHomeSearchResponse
{
    [JsonPropertyName("result")]
    public bool Result { get; set; }

    [JsonPropertyName("data")]
    public MyHomeSearchData Data { get; set; } = new();
}

public class MyHomeSearchData
{
    [JsonPropertyName("data")]
    public List<MyHomeListing> Listings { get; set; } = [];
}

public class MyHomeListing
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("deal_type_id")]
    public int DealTypeId { get; set; }

    [JsonPropertyName("real_estate_type_id")]
    public int RealEstateTypeId { get; set; }

    [JsonPropertyName("status_id")]
    public int? StatusId { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("price")]
    public Dictionary<string, MyHomePrice>? Price { get; set; }

    [JsonPropertyName("price_negotiable")]
    public bool? PriceNegotiable { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lng")]
    public double? Lng { get; set; }

    [JsonPropertyName("images")]
    public List<MyHomeImage>? Images { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("area")]
    public double? Area { get; set; }

    [JsonPropertyName("yard_area")]
    public double? YardArea { get; set; }

    [JsonPropertyName("area_type_id")]
    public int? AreaTypeId { get; set; }

    [JsonPropertyName("bedroom")]
    public string? Bedroom { get; set; }

    [JsonPropertyName("room")]
    public string? Room { get; set; }

    [JsonPropertyName("floor")]
    public int? Floor { get; set; }

    [JsonPropertyName("total_floors")]
    public int? TotalFloors { get; set; }

    [JsonPropertyName("street_id")]
    public int? StreetId { get; set; }

    [JsonPropertyName("urban_id")]
    public int? UrbanId { get; set; }

    [JsonPropertyName("urban_name")]
    public string? UrbanName { get; set; }

    [JsonPropertyName("district_id")]
    public int? DistrictId { get; set; }

    [JsonPropertyName("district_name")]
    public string? DistrictName { get; set; }

    [JsonPropertyName("city_name")]
    public string? CityName { get; set; }

    [JsonPropertyName("dynamic_title")]
    public string? DynamicTitle { get; set; }

    [JsonPropertyName("dynamic_slug")]
    public string? DynamicSlug { get; set; }

    [JsonPropertyName("middle_slug")]
    public string? MiddleSlug { get; set; }

    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("is_vip")]
    public bool? IsVip { get; set; }

    [JsonPropertyName("is_vip_plus")]
    public bool? IsVipPlus { get; set; }

    [JsonPropertyName("is_super_vip")]
    public bool? IsSuperVip { get; set; }

    [JsonPropertyName("is_promoted")]
    public bool? IsPromoted { get; set; }

    [JsonPropertyName("user_title")]
    public string? UserTitle { get; set; }

    [JsonPropertyName("user_type")]
    public MyHomeUserType? UserType { get; set; }

    [JsonPropertyName("has_3d")]
    public bool? Has3d { get; set; }

    [JsonPropertyName("favorite")]
    public bool? Favorite { get; set; }

    // Computed helpers
    public decimal PriceUsd => Price?.TryGetValue("2", out var p) == true ? p.PriceTotal : 0;
    public decimal PriceGel => Price?.TryGetValue("1", out var p) == true ? p.PriceTotal : 0;
    public decimal UnitPriceUsd => Price?.TryGetValue("2", out var p) == true ? p.PriceSquare : 0;

    public string MainImageUrl =>
        Images?.FirstOrDefault(i => i.IsMain)?.Large
        ?? Images?.FirstOrDefault()?.Large
        ?? string.Empty;

    public string DetailPageUrl => $"https://www.myhome.ge/pr/{Id}/{DynamicSlug}";

    public string DisplayAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Address)) parts.Add(Address);
            if (!string.IsNullOrEmpty(UrbanName)) parts.Add(UrbanName);
            if (!string.IsNullOrEmpty(CityName)) parts.Add(CityName);
            return string.Join(", ", parts);
        }
    }
}

public class MyHomePrice
{
    [JsonPropertyName("price_total")]
    public decimal PriceTotal { get; set; }

    [JsonPropertyName("price_square")]
    public decimal PriceSquare { get; set; }
}

public class MyHomeImage
{
    [JsonPropertyName("large")]
    public string? Large { get; set; }

    [JsonPropertyName("thumb")]
    public string? Thumb { get; set; }

    [JsonPropertyName("blur")]
    public string? Blur { get; set; }

    [JsonPropertyName("is_main")]
    public bool IsMain { get; set; }
}

public class MyHomeUserType
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}
