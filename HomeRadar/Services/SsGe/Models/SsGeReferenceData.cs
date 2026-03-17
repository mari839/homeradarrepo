using System.Text.Json.Serialization;

namespace HomeRadar.Services.SsGe.Models;

public record SsGeCityDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("title")] string Title
);

public record SsGeDistrictDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("subDistricts")] List<SsGeSubDistrictDto> SubDistricts
);

public record SsGeSubDistrictDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title
);

public record SsGeMunicipalityDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("title")] string Title
);

public record SsGeTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType
);
