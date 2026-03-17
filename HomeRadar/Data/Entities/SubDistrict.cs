using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeRadar.Data.Entities;

public class SubDistrict
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int DistrictId { get; set; }

    [ForeignKey(nameof(DistrictId))]
    public District District { get; set; } = null!;
}
