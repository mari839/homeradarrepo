using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeRadar.Data.Entities;

public class SavedFilter
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SearchRequestJson { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TelegramChatId { get; set; }

    [MaxLength(20)]
    public string? TelegramLinkCode { get; set; }

    public int? LastSeenApplicationId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastCheckedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
