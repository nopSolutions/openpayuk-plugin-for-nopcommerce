namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a OpenPay region
    /// </summary>
    public class Region
    {
        #region Properties

        /// <summary>
        /// Gets or sets a widget code
        /// </summary>
        public string WidgetCode { get; set; }

        /// <summary>
        /// Gets or sets a two letter ISO code
        /// </summary>
        public string TwoLetterIsoCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sandbox environment is active
        /// </summary>
        public bool IsSandbox { get; set; }

        /// <summary>
        /// Gets or sets the API URL
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the handover URL to redirect user to payment gateway
        /// </summary>
        public string HandoverUrl { get; set; }

        /// <summary>
        /// Gets or sets the currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        #endregion
    }
}
