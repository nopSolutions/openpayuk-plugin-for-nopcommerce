using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a request for the creation of a new order
    /// </summary>
    public class CreateOrderRequest
    {
        #region Properties

        /// <summary>
        /// Gets or sets the details of the customer journey
        /// </summary>
        [JsonProperty("customerJourney")]
        public CustomerJourney CustomerJourney { get; set; }

        /// <summary>
        /// Gets or sets the purchase price of the order. 
        /// An integer number in the lowest denomination in the currency being used (e.g. supply 1034 to indicate $10.34)
        /// </summary>
        [JsonProperty("purchasePrice")]
        public int PurchasePrice { get; set; }

        /// <summary>
        /// Gets or sets the retailer reference (e.g. order/invoice) number for this order
        /// </summary>
        [JsonProperty("retailerOrderNo")]
        public string RetailerOrderNo { get; set; }

        /// <summary>
        /// Gets or sets the array of the items being purchased in the order
        /// </summary>
        [JsonProperty("cart")]
        public CartItem[] CartItems { get; set; }

        #endregion
    }
}
