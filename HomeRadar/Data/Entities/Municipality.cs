using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeRadar.Data.Entities;

public class Municipality
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int Order { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
}
