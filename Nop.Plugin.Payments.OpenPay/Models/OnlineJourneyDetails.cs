using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a details of an Online journey
    /// </summary>
    public class OnlineJourneyDetails
    {
        #region Properties

        /// <summary>
        /// Gets or sets the URL to redirect to upon successful plan creation or lodgement (may also serve as the only one, as it has parameter ‘status’: SUCCESS, LODGED, CANCELLED, FAILURE)
        /// </summary>
        [JsonProperty("callbackUrl")]
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to redirect to when user cancels the plan creation or lodgement
        /// </summary>
        [JsonProperty("cancelUrl")]
        public string CancelUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to redirect to when a system error occurs
        /// </summary>
        [JsonProperty("failUrl")]
        public string FailUrl { get; set; }

        /// <summary>
        /// Gets or sets the plan creation type. The value should be “pending” to create the plan in Pending state
        /// </summary>
        [JsonProperty("planCreationType")]
        public string PlanCreationType { get; set; }

        /// <summary>
        /// Gets or sets the delivery method ("Delivery" or "Pickup")
        /// </summary>
        [JsonProperty("deliveryMethod")]
        public string DeliveryMethod { get; set; }

        /// <summary>
        /// Gets or sets the customer's personal and contact details
        /// </summary>
        [JsonProperty("customerDetails")]
        public PersonalDetails CustomerDetails { get; set; }

        #endregion
    }
}
