using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Constants
{
    public enum BillingPeriod
    {
        daily,
        monthly,
        biannually, //6 months
        yearly,
        biennially, //2 years
        forever,
    }

    public enum BillingStatus
    {
        Inactive,
        Starter,
        PremiumTrial,
        ActiveRecurring,
        ActiveLifetime,
        Cancelled,
        Expired,
    }
    public enum ProductTier
    {
        Starter,
        Creator,
        Studio
    }

    public static class BillingExtensions
    {
        public static string ToFriendlyString(this BillingPeriod period)
        {
            return period switch
            {
                BillingPeriod.daily => "Daily",
                BillingPeriod.monthly => "Monthly",
                BillingPeriod.biannually => "Biannually",
                BillingPeriod.yearly => "Yearly",
                BillingPeriod.biennially => "Biennially",
                BillingPeriod.forever => "Forever",
                _ => "Unknown",
            };
        }

        public static int ToMonths(this BillingPeriod period)
        {
            return period switch
            {
                BillingPeriod.daily => 0, // Not applicable
                BillingPeriod.monthly => 1,
                BillingPeriod.biannually => 6,
                BillingPeriod.yearly => 12,
                BillingPeriod.biennially => 24,
                BillingPeriod.forever => int.MaxValue, // Representing indefinite period
                _ => 0,
            };
        }
        public static string ToFriendlyString(this BillingStatus period)
        {
            return period switch
            {
                BillingStatus.Inactive => "Inactive",
                BillingStatus.Starter => "Starter",
                BillingStatus.PremiumTrial => "Premium Trial",
                BillingStatus.ActiveRecurring => "Active Recurring",
                BillingStatus.ActiveLifetime => "Active Lifetime",
                BillingStatus.Cancelled => "Cancelled",
                BillingStatus.Expired => "Expired",
                _ => "Unknown",
            };
        }
        public static bool IsRecurring(this BillingPeriod period)
        {
            return period is BillingPeriod.daily
                or BillingPeriod.monthly
                or BillingPeriod.biannually
                or BillingPeriod.yearly
                or BillingPeriod.biennially;
        }
        public static bool IsEligibleForAiFeatures(this BillingPeriod period, BillingStatus status)
        {
            return period == BillingPeriod.monthly && status == BillingStatus.ActiveRecurring;
        }
    }
}
