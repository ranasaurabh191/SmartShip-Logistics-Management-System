namespace SmartShip.TrackingService.Core.Interfaces.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}