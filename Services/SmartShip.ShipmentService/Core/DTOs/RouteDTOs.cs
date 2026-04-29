namespace SmartShip.ShipmentService.Core.DTOs;

/// <summary>Response DTO for a single route stop.</summary>
public record RouteStopDto(
    int Id, int ShipmentId, int? HubId, string HubName, string HubCity,
    double Latitude, double Longitude, int SequenceOrder,
    bool IsCompleted, DateTime? ReachedAt
);

/// <summary>Hub info received from AdminService for route planning.</summary>
public record HubInfo(
    int Id, string Name, string City, string State, string Country,
    double Latitude, double Longitude
);
