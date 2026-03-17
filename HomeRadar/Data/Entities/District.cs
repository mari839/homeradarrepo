using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeRadar.Data.Entities;

public class District
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int CityId { get; set; }

    [ForeignKey(nameof(CityId))]
    public City City { get; set; } = null!;

    public ICollection<SubDistrict> SubDistricts { get; set; } = [];
}
