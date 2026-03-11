using System.ComponentModel.DataAnnotations;

public class Thing
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string OwnerId { get; set; } = string.Empty;
}
