using System.Text.Json.Serialization;

namespace HomeRadar.Services.MyHome.Models;

public class MyHomeCitiesResponse
{
    [JsonPropertyName("data")]
    public List<MyHomeCityDto> Data { get; set; } = [];
}

public class MyHomeCityDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("districts")]
    public List<MyHomeDistrictDto>? Districts { get; set; }
}

public class MyHomeDistrictDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("urbans")]
    public List<MyHomeUrbanDto>? Urbans { get; set; }
}

public class MyHomeUrbanDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }
}

public class MyHomeParametersResponse
{
    [JsonPropertyName("result")]
    public bool Result { get; set; }

    [JsonPropertyName("data")]
    public MyHomeParametersData? Data { get; set; }
}

public class MyHomeParametersData
{
    [JsonPropertyName("deal_types")]
    public List<MyHomeEnumItem>? DealTypes { get; set; }

    [JsonPropertyName("real_estate_types")]
    public List<MyHomeEnumItem>? RealEstateTypes { get; set; }

    [JsonPropertyName("currencies")]
    public List<MyHomeCurrencyItem>? Currencies { get; set; }

    [JsonPropertyName("conditions")]
    public List<MyHomeEnumItem>? Conditions { get; set; }

    [JsonPropertyName("heating_types")]
    public List<MyHomeEnumItem>? HeatingTypes { get; set; }

    [JsonPropertyName("hot_water_types")]
    public List<MyHomeEnumItem>? HotWaterTypes { get; set; }

    [JsonPropertyName("parking_types")]
    public List<MyHomeEnumItem>? ParkingTypes { get; set; }

    [JsonPropertyName("area_types")]
    public List<MyHomeEnumItem>? AreaTypes { get; set; }

    [JsonPropertyName("room_types")]
    public List<MyHomeRoomType>? RoomTypes { get; set; }

    [JsonPropertyName("bedroom_types")]
    public List<MyHomeEnumItem>? BedroomTypes { get; set; }

    [JsonPropertyName("statuses")]
    public List<MyHomeEnumItem>? Statuses { get; set; }

    [JsonPropertyName("build_years")]
    public List<MyHomeEnumItem>? BuildYears { get; set; }

    [JsonPropertyName("material_types")]
    public List<MyHomeEnumItem>? MaterialTypes { get; set; }
}

public class MyHomeEnumItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

public class MyHomeCurrencyItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

public class MyHomeRoomType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("bedroom_types")]
    public List<MyHomeEnumItem>? BedroomTypes { get; set; }
}
