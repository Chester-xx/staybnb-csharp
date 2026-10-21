using Microsoft.Extensions.Options;

namespace staybnb_csharp.Services
{
    // Pricing options (fee percentages, defaults overridable from configuration)
    public class PricingOptions
    {
        public decimal CleaningFeePercent { get; set; } = 5;
        public decimal ServiceFeePercent { get; set; } = 15;
    }

    // Price breakdown record
    public record PriceBreakdown(int Nights, decimal Subtotal, decimal CleaningFee, decimal ServiceFee, decimal Total);

    // Pricing service interface
    public interface IPricingService
    {
        // Properties
        decimal CleaningFeePercent { get; }
        decimal ServiceFeePercent { get; }

        // Methods
        PriceBreakdown Calculate(decimal pricePerNight, int nights);
    }

    // Pricing service
    public class PricingService(IOptions<PricingOptions> options) : IPricingService
    {
        // Fee percentages from the pricing options
        public decimal CleaningFeePercent => options.Value.CleaningFeePercent;
        public decimal ServiceFeePercent => options.Value.ServiceFeePercent;

        // Define calculate method, total = (price per night x nights) + cleaning fee + service fee
        public PriceBreakdown Calculate(decimal pricePerNight, int nights)
        {
            // Calculate the subtotal, cleaning fee and service fee
            var subtotal = Round(pricePerNight * nights);
            var cleaningFee = Round(subtotal * CleaningFeePercent / 100m);
            var serviceFee = Round(subtotal * ServiceFeePercent / 100m);

            // Return the breakdown with the total
            return new PriceBreakdown(nights, subtotal, cleaningFee, serviceFee, subtotal + cleaningFee + serviceFee);
        }

        // Define round method, 2 decimal places, midpoint rounds away from zero
        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
