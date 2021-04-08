using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.OpenPay
{
    /// <summary>
    /// Represents settings of the OpenPay payment plugin
    /// </summary>
    public class OpenPayPaymentSettings : ISettings
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to sandbox environment is active
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets the token to sign the API requests
        /// </summary>
        public string ApiToken { get; set; }

        /// <summary>
        /// Gets or sets a region two letter ISO code
        /// </summary>
        public string RegionTwoLetterIsoCode { get; set; }

        /// <summary>
        /// Gets or sets the min order total
        /// </summary>
        public int MinOrderTotal { get; set; }

        /// <summary>
        /// Gets or sets the max order total
        /// </summary>
        public int MaxOrderTotal { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the price breakdown should be displayed on product page.
        /// </summary>
        public bool DisplayPriceBreakdownOnProductPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the price breakdown should be displayed on product box.
        /// </summary>
        public bool DisplayPriceBreakdownInProductBox { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the price breakdown should be displayed on shopping cart.
        /// </summary>
        public bool DisplayPriceBreakdownInShoppingCart { get; set; }

        /// <summary>
        /// Gets or sets the available plan tiers you have available in months. E.g. [2,4,6] for 2 months, 4 months and 6 months
        /// </summary>
        public string PlanTiers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to log callback errors after successful payment when user is redirect to merchant site
        /// </summary>
        public bool LogCallbackErrors { get; set; }

        #endregion
    }
}
