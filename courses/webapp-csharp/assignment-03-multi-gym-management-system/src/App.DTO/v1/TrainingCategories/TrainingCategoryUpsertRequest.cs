using App.Domain.Enums;

namespace App.DTO.v1.TrainingCategories;

public class TrainingCategoryUpsertRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
