using Microsoft.Extensions.Options;

namespace staybnb_csharp.Services
{
    public class PricingOptions
    {
        public decimal CleaningFeePercent { get; set; } = 5;
        public decimal ServiceFeePercent { get; set; } = 15;
    }

    public record PriceBreakdown(int Nights, decimal Subtotal, decimal CleaningFee, decimal ServiceFee, decimal Total);

    public interface IPricingService
    {
        decimal CleaningFeePercent { get; }
        decimal ServiceFeePercent { get; }

        /// <summary>TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee</summary>
        PriceBreakdown Calculate(decimal pricePerNight, int nights);
    }

    public class PricingService(IOptions<PricingOptions> options) : IPricingService
    {
        public decimal CleaningFeePercent => options.Value.CleaningFeePercent;
        public decimal ServiceFeePercent => options.Value.ServiceFeePercent;

        public PriceBreakdown Calculate(decimal pricePerNight, int nights)
        {
            var subtotal = Round(pricePerNight * nights);
            var cleaningFee = Round(subtotal * CleaningFeePercent / 100m);
            var serviceFee = Round(subtotal * ServiceFeePercent / 100m);

            return new PriceBreakdown(nights, subtotal, cleaningFee, serviceFee, subtotal + cleaningFee + serviceFee);
        }

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
