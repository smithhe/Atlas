namespace Atlas.Application.Features.AzureDevOps.Connection;

public sealed record UpdateAzureConnectionCommand(
    string Organization,
    string Project,
    string AreaPath,
    string? TeamName,
    bool IsEnabled) : IRequest<bool>;
