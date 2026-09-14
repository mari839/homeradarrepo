using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeRadar.Data.Entities;

public class MyHomeCity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public ICollection<MyHomeDistrict> Districts { get; set; } = [];
}

public class MyHomeDistrict
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int CityId { get; set; }

    [ForeignKey(nameof(CityId))]
    public MyHomeCity City { get; set; } = null!;

    public ICollection<MyHomeUrban> Urbans { get; set; } = [];
}

public class MyHomeUrban
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int DistrictId { get; set; }

    [ForeignKey(nameof(DistrictId))]
    public MyHomeDistrict District { get; set; } = null!;
}
