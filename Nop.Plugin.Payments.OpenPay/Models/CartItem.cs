using Newtonsoft.Json;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a item being purchased in the order
    /// </summary>
    public class CartItem
    {
        #region Properties

        /// <summary>
        /// Gets or sets the description of the item used by the retailer
        /// </summary>
        [JsonProperty("itemName")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the internal stock number for this item
        /// </summary>
        [JsonProperty("itemCode")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the individual retail price charged for the item An integer number in the lowest denomination in the currency being used (e.g. 1034 indicates $10.34)
        /// </summary>
        [JsonProperty("itemRetailUnitPrice")]
        public int UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        [JsonProperty("itemQty")]
        public string Quantity { get; set; }

        /// <summary>
        /// Gets or sets the overall retail charge for the quantity of items An integer number in the lowest denomination in the currency being used (e.g. 1034 indicates $10.34)
        /// </summary>
        [JsonProperty("itemRetailCharge")]
        public int Charge { get; set; }

        #endregion
    }
}
