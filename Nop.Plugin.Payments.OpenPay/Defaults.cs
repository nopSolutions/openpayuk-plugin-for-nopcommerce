using Nop.Core;
using Nop.Plugin.Payments.OpenPay.Models;

namespace Nop.Plugin.Payments.OpenPay
{
    /// <summary>
    /// Represents a plugin defaults
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Gets the plugin system name
        /// </summary>
        public static string SystemName => "Payments.OpenPay";

        /// <summary>
        /// Gets the plugin configuration route name
        /// </summary>
        public static string ConfigurationRouteName => "Plugin.Payments.OpenPay.Configure";

        /// <summary>
        /// Gets the callback route name after successful payment
        /// </summary>
        public static string SuccessfulPaymentWebhookRouteName => "OpenPaySuccessfulPaymentWebhook";
        
        /// <summary>
        /// Gets the callback OpenPay landing page route name
        /// </summary>
        public static string OpenPayLandingPageRouteName => "OpenPayLandingPage";

        /// <summary>
        /// Gets the order details route name
        /// </summary>
        public static string OrderDetailsRouteName => "OrderDetails";

        /// <summary>
        /// Gets the shopping cart route name
        /// </summary>
        public static string ShoppingCartRouteName => "ShoppingCart";

        /// <summary>
        /// Gets a name of the view component to display the OpenPay widget in public store
        /// </summary>
        public const string WIDGET_VIEW_COMPONENT_NAME = "OpenPayWidget";

        /// <summary>
        /// Gets a name of the view component to display the OpenPay payment information in public store
        /// </summary>
        public const string PAYMENT_INFO_VIEW_COMPONENT_NAME = "OpenPayPaymentInformation";

        /// <summary>
        /// Gets a name of the order limits schedule task
        /// </summary>
        public static string OrderLimitsTaskName => "Order limits (OpenPay plugin)";

        /// <summary>
        /// Gets a type of the order limits schedule task
        /// </summary>
        public static string OrderLimitsTask => "Nop.Plugin.Payments.OpenPay.Services.OrderLimitsTask";

        /// <summary>
        /// Gets a default period in seconds to run the order limits task
        /// </summary>
        public static int DefaultOrderLimitsTaskPeriodInSeconds => 24 * 60 * 60;

        /// <summary>
        /// Represents a OpenPay defaults
        /// </summary>
        /// <remarks>
        /// Useful links
        /// https://developer.openpay.co.uk/api.html
        /// https://github.com/openpay-innovations/magento2-openpaysdk/blob/main/config/config.ini
        /// https://widgets.openpay.com.au/config
        /// </remarks>
        public static class OpenPay
        {
            /// <summary>
            /// Gets the user agent
            /// </summary>
            public static Region[] AvailableRegions => new[]
            {
                new Region
                {
                    WidgetCode = "AU",
                    TwoLetterIsoCode = "AU",
                    IsSandbox = true,
                    ApiUrl = "https://api.training.myopenpay.com.au",
                    HandoverUrl = "https://retailer.myopenpay.com.au/websalestraining",
                    CurrencyCode = "AUD"
                },
                new Region
                {
                    WidgetCode = "AU",
                    TwoLetterIsoCode = "AU",
                    IsSandbox = false,
                    ApiUrl = "https://api.myopenpay.com.au",
                    HandoverUrl = "https://retailer.myopenpay.com.au/websaleslive",
                    CurrencyCode = "AUD"
                },
                new Region
                {
                    WidgetCode = "UK",
                    TwoLetterIsoCode = "GB",
                    IsSandbox = true,
                    ApiUrl = "https://api.training.myopenpay.co.uk",
                    HandoverUrl = "https://websales.training.myopenpay.co.uk",
                    CurrencyCode = "GBP"
                },
                new Region
                {
                    WidgetCode = "UK",
                    TwoLetterIsoCode = "GB",
                    IsSandbox = false,
                    ApiUrl = "https://api.myopenpay.co.uk",
                    HandoverUrl = "https://websales.myopenpay.co.uk",
                    CurrencyCode = "GBP"
                }
            };

            /// <summary>
            /// Represents a API defaults
            /// </summary>
            public static class Api
            {
                /// <summary>
                /// Gets the user agent
                /// </summary>
                public static string UserAgent => $"nopCommerce-{NopVersion.CurrentVersion}";

                /// <summary>
                /// Gets the default timeout
                /// </summary>
                public static int DefaultTimeout => 20;

                /// <summary>
                /// Gets the version header name
                /// </summary>
                public static string VersionHeaderName => "openpay-version";

                /// <summary>
                /// Gets the version
                /// </summary>
                public static string Version => "1.20201120";
            }

            /// <summary>
            /// Represents widget defaults
            /// </summary>
            public static class Widget
            {
                /// <summary>
                /// Gets the script URL
                /// </summary>
                public static string ScriptUrl => "https://widgets.openpay.com.au/lib/openpay-widgets.min.js";
            }
        }
    }
}
