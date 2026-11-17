using Claims.Models;

namespace Claims.Services;

public class PremiumCalculator
{
    private const decimal BaseDayRate = 1250m;

    public decimal Calculate(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be earlier than the start date.", nameof(endDate));
        }

        var totalDays = (int)Math.Floor((endDate - startDate).TotalDays);
        if (totalDays <= 0)
        {
            return 0m;
        }

        var baseMultiplier = GetBaseMultiplier(coverType);
        var totalPremium = 0m;

        var firstPeriodDays = Math.Min(totalDays, 30);
        totalPremium += firstPeriodDays * BaseDayRate * baseMultiplier;

        var secondPeriodDays = Math.Min(Math.Max(totalDays - 30, 0), 150);
        totalPremium += secondPeriodDays * BaseDayRate * baseMultiplier * GetSecondPeriodDiscount(coverType);

        var remainingDays = Math.Max(totalDays - 180, 0);
        totalPremium += remainingDays * BaseDayRate * baseMultiplier * GetRemainingDaysDiscount(coverType);

        return totalPremium;
    }

    private static decimal GetBaseMultiplier(CoverType coverType)
    {
        return coverType switch
        {
            CoverType.Yacht => 1.1m,
            CoverType.PassengerShip => 1.2m,
            CoverType.Tanker => 1.5m,
            _ => 1.3m
        };
    }

    private static decimal GetSecondPeriodDiscount(CoverType coverType)
    {
        return coverType == CoverType.Yacht ? 0.95m : 0.98m;
    }

    private static decimal GetRemainingDaysDiscount(CoverType coverType)
    {
        return coverType == CoverType.Yacht ? 0.92m : 0.97m;
    }
}
