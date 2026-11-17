using Claims.Auditing;
using Claims.Data;
using Claims.Models;
using Microsoft.EntityFrameworkCore;

namespace Claims.Services;

public interface IClaimService
{
    Task<IReadOnlyCollection<Claim>> GetAllAsync();
    Task<Claim?> GetByIdAsync(string id);
    Task<Claim> CreateAsync(Claim claim);
    Task DeleteAsync(string id);
    Task ValidateClaimAsync(Claim claim, Cover? cover);
}

public class ClaimService : IClaimService
{
    private readonly ClaimsContext _context;
    private readonly IAuditQueue _auditQueue;

    public ClaimService(ClaimsContext context, IAuditQueue auditQueue)
    {
        _context = context;
        _auditQueue = auditQueue;
    }

    public async Task<IReadOnlyCollection<Claim>> GetAllAsync()
    {
        return await _context.Claims.AsNoTracking().OrderBy(x => x.Created).ToListAsync();
    }

    public async Task<Claim?> GetByIdAsync(string id)
    {
        return await _context.Claims.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Claim> CreateAsync(Claim claim)
    {
        var cover = await _context.Covers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == claim.CoverId);
        await ValidateClaimAsync(claim, cover);

        claim.Id = string.IsNullOrWhiteSpace(claim.Id) ? Guid.NewGuid().ToString() : claim.Id;
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();

        _auditQueue.Enqueue(new AuditEntry(nameof(Claim), claim.Id, "POST", DateTime.UtcNow));
        return claim;
    }

    public async Task DeleteAsync(string id)
    {
        var claim = await _context.Claims.SingleOrDefaultAsync(x => x.Id == id);
        if (claim is null)
        {
            return;
        }

        _context.Claims.Remove(claim);
        await _context.SaveChangesAsync();
        _auditQueue.Enqueue(new AuditEntry(nameof(Claim), id, "DELETE", DateTime.UtcNow));
    }

    public async Task ValidateClaimAsync(Claim claim, Cover? cover)
    {
        if (claim.DamageCost > 100_000m)
        {
            throw new ArgumentException("Damage cost cannot exceed 100,000.", nameof(claim.DamageCost));
        }

        if (cover is null)
        {
            throw new ArgumentException("A valid cover must be associated with the claim.", nameof(claim.CoverId));
        }

        if (claim.Created < cover.StartDate || claim.Created > cover.EndDate)
        {
            throw new ArgumentException("Claim created date must fall within the related cover period.", nameof(claim.Created));
        }
    }
}
