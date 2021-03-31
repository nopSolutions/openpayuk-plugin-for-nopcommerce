using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a API error.
    /// </summary>
    public class ApiError
    {
        #region Properties

        /// <summary>
        /// Gets or sets the error type.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the error title.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the error status.
        /// </summary>
        [JsonProperty("status")]
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets the error detail.
        /// </summary>
        [JsonProperty("detail")]
        public string Detail { get; set; }

        /// <summary>
        /// Gets or sets the error instance.
        /// </summary>
        [JsonProperty("instance")]
        public string Instance { get; set; }

        #endregion
    }
}
