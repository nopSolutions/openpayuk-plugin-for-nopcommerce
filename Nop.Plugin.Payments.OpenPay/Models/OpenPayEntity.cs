using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a OpenPay entity
    /// </summary>
    public class OpenPayEntity
    {
        #region Properties

        /// <summary>
        /// Gets or sets the order id
        /// </summary>
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        #endregion
    }
}
