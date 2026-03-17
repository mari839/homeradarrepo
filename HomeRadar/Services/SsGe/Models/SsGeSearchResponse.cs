using System.Text.Json.Serialization;

namespace HomeRadar.Services.SsGe.Models;

public class SsGeSearchResponse
{
    [JsonPropertyName("realStateItemModel")]
    public List<SsGeListing> Listings { get; set; } = [];

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public class SsGeListing
{
    [JsonPropertyName("applicationId")]
    public int ApplicationId { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("address")]
    public SsGeAddress Address { get; set; } = new();

    [JsonPropertyName("price")]
    public SsGePrice Price { get; set; } = new();

    [JsonPropertyName("appImages")]
    public List<SsGeImage> AppImages { get; set; } = [];

    [JsonPropertyName("imageCount")]
    public int ImageCount { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("shortTitle")]
    public string? ShortTitle { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("totalArea")]
    public double? TotalArea { get; set; }

    [JsonPropertyName("totalAmountOfFloor")]
    public double? TotalAmountOfFloor { get; set; }

    [JsonPropertyName("floorNumber")]
    public string? FloorNumber { get; set; }

    [JsonPropertyName("numberOfBedrooms")]
    public int? NumberOfBedrooms { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("dealType")]
    public int DealType { get; set; }

    [JsonPropertyName("isMovedUp")]
    public bool IsMovedUp { get; set; }

    [JsonPropertyName("isExclusive")]
    public bool IsExclusive { get; set; }

    [JsonPropertyName("isHighlighted")]
    public bool IsHighlighted { get; set; }

    [JsonPropertyName("isUrgent")]
    public bool IsUrgent { get; set; }

    [JsonPropertyName("vipStatus")]
    public int VipStatus { get; set; }

    [JsonPropertyName("hasRemoteViewing")]
    public bool HasRemoteViewing { get; set; }

    [JsonPropertyName("videoLink")]
    public string? VideoLink { get; set; }

    [JsonPropertyName("commercialRealEstateType")]
    public int CommercialRealEstateType { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTime? OrderDate { get; set; }

    [JsonPropertyName("createDate")]
    public DateTime? CreateDate { get; set; }

    [JsonPropertyName("detailUrl")]
    public string? DetailUrl { get; set; }

    [JsonPropertyName("userInfo")]
    public SsGeUserInfo? UserInfo { get; set; }

    [JsonPropertyName("furniture")]
    public bool? Furniture { get; set; }

    [JsonPropertyName("withBuiltInKitchen")]
    public bool? WithBuiltInKitchen { get; set; }

    [JsonPropertyName("nearbySubwayStations")]
    public List<SsGeSubwayStation>? NearbySubwayStations { get; set; }

    public string MainImageUrl =>
        AppImages.FirstOrDefault(i => i.IsMain)?.FileName
        ?? AppImages.FirstOrDefault()?.FileName
        ?? string.Empty;

    public string DetailPageUrl => $"https://home.ss.ge/ka/udzravi-qoneba/{DetailUrl}";
}

public class SsGeAddress
{
    [JsonPropertyName("municipalityId")]
    public int? MunicipalityId { get; set; }

    [JsonPropertyName("municipalityTitle")]
    public string? MunicipalityTitle { get; set; }

    [JsonPropertyName("cityId")]
    public int? CityId { get; set; }

    [JsonPropertyName("cityTitle")]
    public string? CityTitle { get; set; }

    [JsonPropertyName("districtId")]
    public int? DistrictId { get; set; }

    [JsonPropertyName("districtTitle")]
    public string? DistrictTitle { get; set; }

    [JsonPropertyName("subdistrictId")]
    public int? SubdistrictId { get; set; }

    [JsonPropertyName("subdistrictTitle")]
    public string? SubdistrictTitle { get; set; }

    [JsonPropertyName("streetId")]
    public int? StreetId { get; set; }

    [JsonPropertyName("streetTitle")]
    public string? StreetTitle { get; set; }

    [JsonPropertyName("streetNumber")]
    public string? StreetNumber { get; set; }

    public string DisplayAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(StreetTitle)) parts.Add(StreetTitle);
            if (!string.IsNullOrEmpty(SubdistrictTitle)) parts.Add(SubdistrictTitle);
            if (!string.IsNullOrEmpty(DistrictTitle)) parts.Add(DistrictTitle);
            if (!string.IsNullOrEmpty(CityTitle)) parts.Add(CityTitle);
            return string.Join(", ", parts);
        }
    }
}

public class SsGePrice
{
    [JsonPropertyName("priceGeo")]
    public decimal? PriceGeo { get; set; }

    [JsonPropertyName("unitPriceGeo")]
    public decimal? UnitPriceGeo { get; set; }

    [JsonPropertyName("priceUsd")]
    public decimal? PriceUsd { get; set; }

    [JsonPropertyName("unitPriceUsd")]
    public decimal? UnitPriceUsd { get; set; }

    [JsonPropertyName("currencyType")]
    public int CurrencyType { get; set; }
}

public class SsGeImage
{
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("isMain")]
    public bool IsMain { get; set; }

    [JsonPropertyName("is360")]
    public bool Is360 { get; set; }

    [JsonPropertyName("orderNo")]
    public int OrderNo { get; set; }
}

public class SsGeUserInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("userType")]
    public int UserType { get; set; }
}

public class SsGeSubwayStation
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("distance")]
    public double? Distance { get; set; }
}
