using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Settings.GetSettings;

public sealed record GetSettingsQuery() : IRequest<Atlas.Domain.Entities.Settings>;

