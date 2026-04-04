using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Domain.Enums;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly TrackingDbContext _context;

    public DocumentRepository(TrackingDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByShipmentIdAndFileNameAsync(int shipmentId, string fileName)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.ShipmentId == shipmentId && d.FileName == fileName);
    }

    public async Task AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
    }

    public async Task<PagedResponse<Document>> GetPagedByShipmentIdAsync(int shipmentId, DocumentPagedRequest req)
    {
        var query = _context.Documents
            .Where(d => d.ShipmentId == shipmentId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(req.DocumentType) &&
            Enum.TryParse<DocumentType>(req.DocumentType, true, out var dt))
        {
            query = query.Where(d => d.DocumentType == dt);
        }

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(d => d.FileName.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(d => d.UploadedAt)
            : query.OrderByDescending(d => d.UploadedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<Document>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}