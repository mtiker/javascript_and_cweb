using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TrainingCategories;

public class TrainingCategoryUpsertRequest
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = default!;

    [StringLength(512)]
    public string? Description { get; set; }
}
