using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a details of the customer journey
    /// </summary>
    public class CustomerJourney
    {
        #region Properties

        /// <summary>
        /// Gets or sets the type of customer journey being started ("Online" "PosApp" "PosWeb")
        /// </summary>
        [JsonProperty("origin")]
        public string Origin { get; set; }

        /// <summary>
        /// Gets or sets the details of an Online journey. Required if the Origin is set to "Online"
        /// </summary>
        [JsonProperty("online")]
        public OnlineJourneyDetails Online { get; set; }

        #endregion
    }
}
