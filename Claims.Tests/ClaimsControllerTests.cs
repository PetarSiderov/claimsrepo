using Claims.Auditing;
using Claims.Data;
using Claims.Models;
using Claims.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Claims.Tests;

public class ClaimsControllerTests
{
    [Fact]
    public void Premium_ForThirtyDayYachtCover_IsCalculatedUsingBaseRate()
    {
        var calculator = new PremiumCalculator();

        var premium = calculator.Calculate(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), CoverType.Yacht);

        Assert.Equal(30m * 1250m * 1.1m, premium);
    }

    [Fact]
    public void Premium_ForOneYearTankerCover_UsesProgressiveDiscounts()
    {
        var calculator = new PremiumCalculator();

        var premium = calculator.Calculate(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1), CoverType.Tanker);

        Assert.Equal(1250m * 1.5m * 30m + 1250m * 1.5m * 150m * 0.98m + 1250m * 1.5m * 185m * 0.97m, premium);
    }

    [Fact]
    public async Task ClaimService_RejectsDamageCostOverLimit()
    {
        var options = new DbContextOptionsBuilder<ClaimsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ClaimsContext(options);
        var queue = new InMemoryAuditQueue();
        var service = new ClaimService(context, queue);

        var cover = new Cover
        {
            Id = "cover-1",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(100),
            Type = CoverType.Yacht
        };

        context.Covers.Add(cover);
        await context.SaveChangesAsync();

        var claim = new Claim
        {
            CoverId = "cover-1",
            Created = DateTime.UtcNow.AddDays(15),
            DamageCost = 100_001m,
            Name = "Test claim",
            Type = ClaimType.Collision
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(claim));
    }

    [Fact]
    public async Task ClaimService_RejectsClaimOutsideCoverWindow()
    {
        var options = new DbContextOptionsBuilder<ClaimsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ClaimsContext(options);
        var queue = new InMemoryAuditQueue();
        var service = new ClaimService(context, queue);

        var cover = new Cover
        {
            Id = "cover-2",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(100),
            Type = CoverType.PassengerShip
        };

        context.Covers.Add(cover);
        await context.SaveChangesAsync();

        var claim = new Claim
        {
            CoverId = "cover-2",
            Created = DateTime.UtcNow.AddDays(200),
            DamageCost = 50_000m,
            Name = "Outside period",
            Type = ClaimType.Fire
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(claim));
    }

    [Fact]
    public void CoverService_RejectsStartDateInPast()
    {
        var options = new DbContextOptionsBuilder<ClaimsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ClaimsContext(options);
        var service = new CoverService(context, new PremiumCalculator(), new InMemoryAuditQueue());

        var cover = new Cover
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(20),
            Type = CoverType.Yacht
        };

        var ex = Assert.Throws<ArgumentException>(() => service.ValidateCover(cover));
        Assert.Contains("past", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CoverService_RejectsPolicyLongerThanOneYear()
    {
        var options = new DbContextOptionsBuilder<ClaimsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ClaimsContext(options);
        var service = new CoverService(context, new PremiumCalculator(), new InMemoryAuditQueue());

        var cover = new Cover
        {
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(400),
            Type = CoverType.ContainerShip
        };

        var ex = Assert.Throws<ArgumentException>(() => service.ValidateCover(cover));
        Assert.Contains("one year", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryAuditQueue : IAuditQueue
    {
        private Task? _processingTask;

        public void Enqueue(AuditEntry entry)
        {
        }

        public Task DrainAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _processingTask = Task.Run(() => DrainAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processingTask is not null)
            {
                await _processingTask;
            }
        }
    }
}
