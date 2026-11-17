using Claims.Auditing;
using Claims.Data;
using Claims.Models;
using Microsoft.EntityFrameworkCore;

namespace Claims.Services;

public interface ICoverService
{
    Task<IReadOnlyCollection<Cover>> GetAllAsync();
    Task<Cover?> GetByIdAsync(string id);
    Task<Cover> CreateAsync(Cover cover);
    Task DeleteAsync(string id);
    decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType);
}

public class CoverService : ICoverService
{
    private readonly ClaimsContext _context;
    private readonly PremiumCalculator _premiumCalculator;
    private readonly IAuditQueue _auditQueue;

    public CoverService(ClaimsContext context, PremiumCalculator premiumCalculator, IAuditQueue auditQueue)
    {
        _context = context;
        _premiumCalculator = premiumCalculator;
        _auditQueue = auditQueue;
    }

    public async Task<IReadOnlyCollection<Cover>> GetAllAsync()
    {
        return await _context.Covers.AsNoTracking().OrderBy(x => x.StartDate).ToListAsync();
    }

    public async Task<Cover?> GetByIdAsync(string id)
    {
        return await _context.Covers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Cover> CreateAsync(Cover cover)
    {
        ValidateCover(cover);
        cover.Id = string.IsNullOrWhiteSpace(cover.Id) ? Guid.NewGuid().ToString() : cover.Id;
        cover.Premium = ComputePremium(cover.StartDate, cover.EndDate, cover.Type);

        _context.Covers.Add(cover);
        await _context.SaveChangesAsync();

        _auditQueue.Enqueue(new AuditEntry(nameof(Cover), cover.Id, "POST", DateTime.UtcNow));
        return cover;
    }

    public async Task DeleteAsync(string id)
    {
        var cover = await _context.Covers.SingleOrDefaultAsync(x => x.Id == id);
        if (cover is null)
        {
            return;
        }

        _context.Covers.Remove(cover);
        await _context.SaveChangesAsync();
        _auditQueue.Enqueue(new AuditEntry(nameof(Cover), id, "DELETE", DateTime.UtcNow));
    }

    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return _premiumCalculator.Calculate(startDate, endDate, coverType);
    }

    public void ValidateCover(Cover cover)
    {
        if (cover.StartDate <= DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Start date cannot be in the past or today.", nameof(cover.StartDate));
        }

        var totalDays = (cover.EndDate - cover.StartDate).TotalDays;
        if (totalDays > 365)
        {
            throw new ArgumentException("Insurance period cannot exceed one year.", nameof(cover.EndDate));
        }
    }
}
