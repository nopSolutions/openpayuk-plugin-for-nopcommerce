using System;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a API exception
    /// </summary>
    public class ApiException : Exception
    {
        #region Properties

        /// <summary>
        /// Gets or sets the error code (HTTP status code)
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error content.
        /// </summary>
        public ApiError Error { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class.
        /// </summary>
        /// <param name="errorCode">The HTTP status code.</param>
        /// <param name="message">The error message.</param>
        public ApiException(int errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class.
        /// </summary>
        /// <param name="errorCode">The HTTP status code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="error">The error content.</param>
        public ApiException(int errorCode, string message, ApiError error = null)
            : this(errorCode, message)
        {
            Error = error;
        }

        #endregion
    }
}
