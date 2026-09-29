using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Incidents.Domain.Model.Commands;
using BuildingFex.Api.Shared.Application.Model;

namespace BuildingFex.Api.Incidents.Application.CommandServices;

public interface IIncidentCommandService
{
    Task<Result<Incident>> Handle(CreateIncidentCommand command, CancellationToken cancellationToken = default);
    Task<Result<Incident>> Handle(UpdateIncidentCommand command, CancellationToken cancellationToken = default);
    Task<Result<Incident>> Handle(DeleteIncidentCommand command, CancellationToken cancellationToken = default);
}
