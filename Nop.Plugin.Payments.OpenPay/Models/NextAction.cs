using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a next action in the flow and the details necessary to take that action
    /// </summary>
    public class NextAction
    {
        #region Properties

        /// <summary>
        /// Gets or sets the next action to take in the flow. Depending on the type, it may be accompanied by a matching object with details. ("FormPost", "WaitForCustomer")
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the form post action. If the Type is FormPost, Transaction Token along with the Handover URL will be used to redirect the user to Openpay
        /// </summary>
        [JsonProperty("formPost")]
        public FormPostAction FormPost { get; set; }

        #endregion
    }
}
