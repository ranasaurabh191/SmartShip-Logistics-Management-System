using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Domain.Entities;

namespace SmartShip.TrackingService.Core.Interfaces.Repositories;

public interface ITrackingEventRepository
{
    Task<TrackingEvent?> GetRecentDuplicateAsync(string trackingNumber, string status, string location, DateTime sinceUtc);
    Task AddAsync(TrackingEvent trackingEvent);
    Task<PagedResponse<TrackingEvent>> GetByTrackingNumberPagedAsync(string trackingNumber, TrackingEventPagedRequest request);
}