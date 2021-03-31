using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a limits for orders (min/max price)
    /// </summary>
    public class OrderLimits
    {
        #region Properties

        /// <summary>
        /// Gets or sets the minimum allowed for an order An integer number in the lowest denomination in the currency being used (e.g. 1034 indicates $10.34)
        /// </summary>
        [JsonProperty("minPrice")]
        public int MinPrice { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed for an order An integer number in the lowest denomination in the currency being used (e.g. 1034 indicates $10.34)
        /// </summary>
        [JsonProperty("maxPrice")]
        public int MaxPrice { get; set; }

        #endregion
    }
}
