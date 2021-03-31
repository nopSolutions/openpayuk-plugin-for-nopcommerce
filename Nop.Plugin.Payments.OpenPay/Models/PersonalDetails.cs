using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a customer's personal and contact details
    /// </summary>
    public class PersonalDetails
    {
        #region Properties

        /// <summary>
        /// Gets or sets the customer’s first name
        /// </summary>
        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the customer’s family name
        /// </summary>
        [JsonProperty("familyName")]
        public string FamilyName { get; set; }

        /// <summary>
        /// Gets or sets the specific email addreses can be used to achieve particular effects: 
        ///     * success @openpay.co.uk Return to the caller’s website with the status SUCCESS or LODGED (depends on plan creation type)
        ///     * cancelled @openpay.co.uk Return to the caller’s website with the status CANCELLED
        ///     * failure @openpay.co.uk Return to the caller’s website with the status FAILURE
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the delivery address
        /// </summary>
        [JsonProperty("deliveryAddress")]
        public CustomerAddress DeliveryAddress { get; set; }

        #endregion
    }
}
