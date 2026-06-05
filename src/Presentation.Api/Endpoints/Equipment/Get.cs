using Application.UseCases.GetEquipment;
using Core.Domain.EquipmentAggregate;
using FastEndpoints;
using MediatR;
using Presentation.Contracts.Equipment;

namespace Presentation.Api.Endpoints.Equipment;

public sealed class Get : EndpointWithoutRequest<IReadOnlyList<GetEquipmentResponse>>
{
    private readonly IMediator _mediator;

    public Get(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/get-equipment");
        Policies("AppAccess");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List equipment";
            s.Description = "Returns a paginated, filterable list of all equipment.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var locationIdRaw = Query<string?>("LocationId", isRequired: false);
        var statusFilterRaw = Query<string?>("StatusFilter", isRequired: false);
        var skip = Query<int>("Skip", isRequired: false);
        var takeRaw = Query<string?>("Take", isRequired: false);

        LocationId? locationId = null;
        if (!string.IsNullOrEmpty(locationIdRaw) && Guid.TryParse(locationIdRaw, out var locGuid))
            locationId = new LocationId(locGuid);

        EquipmentStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(statusFilterRaw))
        {
            if (!Enum.TryParse<EquipmentStatus>(statusFilterRaw, ignoreCase: true, out var parsedStatus))
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsJsonAsync(new { error = $"Invalid StatusFilter value '{statusFilterRaw}'. Valid values: Available, InUse, Maintenance." }, ct);
                return;
            }
            statusFilter = parsedStatus;
        }

        int? take = null;
        if (!string.IsNullOrEmpty(takeRaw) && int.TryParse(takeRaw, out var parsedTake) && parsedTake > 0)
            take = parsedTake;

        var request = new GetEquipmentRequest(locationId, statusFilter, skip, take);
        var result = await _mediator.Send(request, ct);

        await SendOkAsync(result.Select(GetEquipmentResponse.From).ToList(), ct);
    }
}
