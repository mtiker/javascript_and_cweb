using System.Globalization;
using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.BLL.Services;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.TrainingCategories;
using BuildingBlocks.Mediator;
using Modules.Training.Contracts;

namespace Modules.Training.Application;

internal sealed class ListTrainingCategoriesQueryHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    ITrainingMapper trainingMapper)
    : IRequestHandler<ListTrainingCategoriesQuery, IReadOnlyCollection<TrainingCategoryResponse>>
{
    public async Task<IReadOnlyCollection<TrainingCategoryResponse>> HandleAsync(
        ListTrainingCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member,
            RoleNames.Trainer);

        var categories = await unitOfWork.TrainingCategories.ListByGymAsync(gymId, cancellationToken);
        return trainingMapper.ToCategoryList(categories);
    }
}

internal sealed class CreateTrainingCategoryCommandHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    ITrainingMapper trainingMapper)
    : IRequestHandler<CreateTrainingCategoryCommand, TrainingCategoryResponse>
{
    public async Task<TrainingCategoryResponse> HandleAsync(
        CreateTrainingCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin);

        TrainingCategoryWorkflow.ValidateRequest(request.Request);

        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = TrainingCategoryWorkflow.ToLangStr(request.Request.Name),
            Description = string.IsNullOrWhiteSpace(request.Request.Description)
                ? null
                : TrainingCategoryWorkflow.ToLangStr(request.Request.Description)
        };

        await unitOfWork.TrainingCategories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return trainingMapper.ToCategory(category);
    }

}

internal sealed class UpdateTrainingCategoryCommandHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    ITrainingMapper trainingMapper)
    : IRequestHandler<UpdateTrainingCategoryCommand, TrainingCategoryResponse>
{
    public async Task<TrainingCategoryResponse> HandleAsync(
        UpdateTrainingCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin);

        TrainingCategoryWorkflow.ValidateRequest(request.Request);

        var category = await unitOfWork.TrainingCategories.FindAsync(gymId, request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Training category was not found.");

        category.Name = TrainingCategoryWorkflow.ToLangStr(request.Request.Name);
        category.Description = string.IsNullOrWhiteSpace(request.Request.Description)
            ? null
            : TrainingCategoryWorkflow.ToLangStr(request.Request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return trainingMapper.ToCategory(category);
    }

}

internal sealed class DeleteTrainingCategoryCommandHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService)
    : IRequestHandler<DeleteTrainingCategoryCommand>
{
    public async Task HandleAsync(DeleteTrainingCategoryCommand request, CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin);

        var category = await unitOfWork.TrainingCategories.FindAsync(gymId, request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Training category was not found.");

        unitOfWork.TrainingCategories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal static class TrainingCategoryWorkflow
{
    public static void ValidateRequest(TrainingCategoryUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationAppException("Training category name is required.");
        }
    }

    public static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }
}
