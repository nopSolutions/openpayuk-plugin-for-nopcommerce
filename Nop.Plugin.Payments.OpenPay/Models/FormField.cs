using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a form field
    /// </summary>
    public class FormField
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the hidden field expected in the form post
        /// </summary>
        [JsonProperty("fieldName")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the hidden field expected in the form post
        /// </summary>
        [JsonProperty("fieldValue")]
        public string Value { get; set; }

        #endregion
    }
}
