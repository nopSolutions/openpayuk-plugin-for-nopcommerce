namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a request for the creation of a new refund
    /// </summary>
    public class CreateRefundRequest
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to the order should be refunded fully
        /// </summary>
        public bool FullRefund { get; set; }

        #endregion
    }
}
