using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a request for the creation of a new refund
    /// </summary>
    public class CreateRefundRequest
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to the order should be refunded fully
        /// </summary>
        [JsonProperty("fullRefund")]
        public bool FullRefund { get; set; }

        /// <summary>
        /// Gets or sets a number in the lowest denomination in the currency being used (e.g. supply 1034 to indicate $10.34) Must be zero, or greater than zero and less than the current purchase price for the order ID concerned. This will reduce the current value of a Plan by the nominated amount and helps cater for Split Order situations where the original value of the order is no longer known.
        /// </summary>
        [JsonProperty("reducePriceBy")]
        public int ReducePriceBy { get; set; }

        #endregion
    }
}
