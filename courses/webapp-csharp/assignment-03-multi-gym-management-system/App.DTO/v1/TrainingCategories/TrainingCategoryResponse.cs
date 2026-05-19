using App.Domain.Enums;

namespace App.DTO.v1.TrainingCategories;

public class TrainingCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
