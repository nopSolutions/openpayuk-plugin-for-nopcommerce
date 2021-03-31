using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a customer address
    /// </summary>
    public class CustomerAddress
    {
        #region Properties

        /// <summary>
        /// Gets or sets the first address line
        /// </summary>
        [JsonProperty("line1")]
        public string Line1 { get; set; }

        /// <summary>
        /// Gets or sets the second address line
        /// </summary>
        [JsonProperty("line2")]
        public string Line2 { get; set; }

        /// <summary>
        /// Gets or sets the address suburb, town or county
        /// </summary>
        [JsonProperty("suburb")]
        public string Suburb { get; set; }

        /// <summary>
        /// Gets or sets the address state (case-sensitive). For Australia, it should be the 3 letter abbreviation e.g. VIC, NSW, etc
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the address postcode Format: AUS: NNNN(zero-padded to the left) Format: UK: XXXXXXXX(may have space)
        /// </summary>
        [JsonProperty("postCode")]
        public string PostCode { get; set; }

        #endregion
    }
}
