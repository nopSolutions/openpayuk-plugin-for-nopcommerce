using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a customer order
    /// </summary>
    public class CustomerOrder : OpenPayEntity
    {
        #region Properties

        /// <summary>
        /// Gets or sets the next action in the flow and the details necessary to take that action
        /// </summary>
        [JsonProperty("nextAction")]
        public NextAction NextAction { get; set; }

        #endregion
    }
}
