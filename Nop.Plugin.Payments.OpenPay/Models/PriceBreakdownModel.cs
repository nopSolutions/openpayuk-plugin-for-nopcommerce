using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a model to display the price breakdown in public store
    /// </summary>
    public class PriceBreakdownModel : BaseNopModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets a region code
        /// </summary>
        public string RegionCode { get; set; }

        /// <summary>
        /// Gets or sets a widget code
        /// </summary>
        public string WidgetCode { get; set; }

        /// <summary>
        /// Gets or sets the currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the array of available plan tiers you have available in months. E.g. [2,4,6] for 2 months, 4 months and 6 months
        /// </summary>
        public int[] PlanTiers { get; set; }

        /// <summary>
        /// Gets or sets the minimum eligible amount required before Openpay is eligible e.g 50
        /// </summary>
        public decimal MinEligibleAmount { get; set; }

        /// <summary>
        /// Gets or sets the maximum eligible amount required before Openpay is eligible e.g 1000
        /// </summary>
        public decimal MaxEligibleAmount { get; set; }

        /// <summary>
        /// Gets or sets the type of your store (Online or Instore)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the amount
        /// </summary>
        public decimal Amount { get; set; }

        #endregion
    }
}
