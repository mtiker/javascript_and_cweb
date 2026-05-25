using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.TrainingCategories;

public class TrainingCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
