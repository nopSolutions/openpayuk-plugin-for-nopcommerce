using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a customer order status
    /// </summary>
    public class CustomerOrderStatus : OpenPayEntity
    {
        #region Properties

        /// <summary>
        /// Gets or sets the order status
        /// </summary>
        [JsonProperty("orderStatus")]
        public string OrderStatus { get; set; }

        /// <summary>
        /// Gets or sets the plan status
        /// </summary>
        [JsonProperty("planStatus")]
        public string PlanStatus { get; set; }

        #endregion
    }
}
