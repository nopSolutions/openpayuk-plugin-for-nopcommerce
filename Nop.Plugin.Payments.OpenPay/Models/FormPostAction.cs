using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a form post action. If the Type is FormPost, Transaction Token along with the Handover URL will be used to redirect the user to Openpay
    /// </summary>
    public class FormPostAction
    {
        #region Properties

        /// <summary>
        /// Gets or sets the URL to which to submit the form post
        /// </summary>
        [JsonProperty("formPostUrl")]
        public string FormPostUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to which to submit the form post
        /// </summary>
        [JsonProperty("formFields")]
        public FormField[] FormFields { get; set; }

        #endregion
    }
}
