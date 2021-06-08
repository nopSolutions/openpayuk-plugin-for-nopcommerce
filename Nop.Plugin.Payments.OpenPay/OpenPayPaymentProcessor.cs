using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.OpenPay.Services;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Payments.OpenPay
{
    /// <summary>
    /// Represents the OpenPay payment processor
    /// </summary>
    public class OpenPayPaymentProcessor : BasePlugin, IPaymentMethod, IWidgetPlugin
    {
        #region Fields

        private readonly OpenPayService _openPayService;
        private readonly OpenPayPaymentSettings _openPayPaymentSettings;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IWebHelper _webHelper;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Ctor

        public OpenPayPaymentProcessor(
            OpenPayService openPayService,
            OpenPayPaymentSettings openPayPaymentSettings,
            IActionContextAccessor actionContextAccessor,
            ILocalizationService localizationService,
            ILogger logger,
            IPaymentService paymentService,
            IUrlHelperFactory urlHelperFactory,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService,
            ISettingService settingService,
            IScheduleTaskService scheduleTaskService,
            IWebHelper webHelper,
            WidgetSettings widgetSettings)
        {
            _openPayService = openPayService;
            _openPayPaymentSettings = openPayPaymentSettings;
            _actionContextAccessor = actionContextAccessor;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _urlHelperFactory = urlHelperFactory;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _settingService = settingService;
            _scheduleTaskService = scheduleTaskService;
            _webHelper = webHelper;
            _widgetSettings = widgetSettings;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (postProcessPaymentRequest is null)
                throw new ArgumentNullException(nameof(postProcessPaymentRequest));

            var order = postProcessPaymentRequest.Order;

            var result = _openPayService.PlaceOrderAsync(order).Result;
            if (!string.IsNullOrEmpty(result.HandoverUrl))
                _httpContextAccessor.HttpContext.Response.Redirect(result.HandoverUrl);
            else
            {
                _logger.Error($"{Defaults.SystemName}: {string.Join("\n", result.Errors)}");

                var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
                var failUrl = urlHelper.RouteUrl(Defaults.OrderDetailsRouteName, new { orderId = order.Id }, _webHelper.CurrentRequestProtocol);

                _notificationService.ErrorNotification(
                    _localizationService.GetResource("Plugins.Payments.OpenPay.FailedOrderCreation"));

                _httpContextAccessor.HttpContext.Response.Redirect(failUrl);
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return !_openPayService.CanDisplayPaymentMethod(cart);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _paymentService.CalculateAdditionalFee(cart,
                _openPayPaymentSettings.AdditionalFee, _openPayPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            if (refundPaymentRequest is null)
                throw new ArgumentNullException(nameof(refundPaymentRequest));

            var amountToRefund = refundPaymentRequest.IsPartialRefund
                ? (decimal?)refundPaymentRequest.AmountToRefund
                : null;
            var order = refundPaymentRequest.Order;
            var result = _openPayService.RefundOrderAsync(order, amountToRefund).Result;
            if (result.IsSuccess)
            {
                return new RefundPaymentResult
                {
                    NewPaymentStatus = refundPaymentRequest.IsPartialRefund
                        ? PaymentStatus.PartiallyRefunded
                        : PaymentStatus.Refunded
                };
            }

            return new RefundPaymentResult { Errors = result.Errors };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (!_openPayService.Validate().IsValid)
                return false;

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return _openPayService.Validate().Errors;
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _urlHelperFactory
                .GetUrlHelper(_actionContextAccessor.ActionContext)
                .RouteUrl(Defaults.ConfigurationRouteName);
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return Defaults.PAYMENT_INFO_VIEW_COMPONENT_NAME;
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new OpenPayPaymentSettings
            {
                UseSandbox = true,
                DisplayProductPageWidget = true,
                DisplayProductListingWidget = true,
                DisplayCartWidget = true,
                DisplayInfoBeltWidget = true,
                DisplayLandingPageWidget = true,
                PlanTiers = "2,4,6",
                LogCallbackErrors = true,
                CartWidgetLogo = "grey-on-amberbg",
                InfoBeltWidgetColor = "white",
                ProductListingWidgetLogo = "grey",
                ProductListingHideLogo = false,
                ProductPageWidgetLogo = "grey-on-amberbg",
                ProductPageWidgetLogoPosition = "left"
            });

            if (!_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(Defaults.SystemName);
                _settingService.SaveSetting(_widgetSettings);
            }

            //schedule tasks
            foreach (var task in Defaults.ScheduleTasks)
            {
                if (_scheduleTaskService.GetTaskByType(task.Type) == null)
                    _scheduleTaskService.InsertTask(task);
            }

            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Payments.OpenPay.Fields.UseSandbox"] = "Use sandbox",
                ["Plugins.Payments.OpenPay.Fields.UseSandbox.Hint"] = "Determine whether to use the sandbox environment for testing purposes.",
                ["Plugins.Payments.OpenPay.Fields.ApiToken"] = "API Token",
                ["Plugins.Payments.OpenPay.Fields.ApiToken.Hint"] = "Enter the token to sign the API requests.",
                ["Plugins.Payments.OpenPay.Fields.ApiToken.Required"] = "The API Token is required.",
                ["Plugins.Payments.OpenPay.Fields.CartWidgetLogo"] = "Logo style",
                ["Plugins.Payments.OpenPay.Fields.CartWidgetLogo.Hint"] = "Enter the logo style of the cart widget (e.g. 'grey-on-amberbg' or 'grey').",
                ["Plugins.Payments.OpenPay.Fields.InfoBeltWidgetColor"] = "Color style",
                ["Plugins.Payments.OpenPay.Fields.InfoBeltWidgetColor.Hint"] = "Enter the color style of the info belt widget (e.g. 'amber', 'white' or 'grey').",
                ["Plugins.Payments.OpenPay.Fields.ProductListingWidgetLogo"] = "Logo style",
                ["Plugins.Payments.OpenPay.Fields.ProductListingWidgetLogo.Hint"] = "Enter the logo style of the product listing widget (e.g. 'grey' or 'white').",
                ["Plugins.Payments.OpenPay.Fields.RegionTwoLetterIsoCode"] = "Payment from applicable country",
                ["Plugins.Payments.OpenPay.Fields.RegionTwoLetterIsoCode.Hint"] = "Select the applicable country. Note that the primary store currency must match the currency of the country.",
                ["Plugins.Payments.OpenPay.Fields.RegionTwoLetterIsoCode.Required"] = "The country is required.",
                ["Plugins.Payments.OpenPay.Fields.MinOrderTotal"] = "Minimum order total",
                ["Plugins.Payments.OpenPay.Fields.MinOrderTotal.Hint"] = "The minimum order total. If order total is less the minimum order total then the payment method will be hidden in checkout process.",
                ["Plugins.Payments.OpenPay.Fields.MaxOrderTotal"] = "Maximum order total",
                ["Plugins.Payments.OpenPay.Fields.MaxOrderTotal.Hint"] = "The maximum order total. If order total is greater the maximum order total then the payment method will be hidden in checkout process.",
                ["Plugins.Payments.OpenPay.Fields.AdditionalFee"] = "Additional fee",
                ["Plugins.Payments.OpenPay.Fields.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
                ["Plugins.Payments.OpenPay.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.Payments.OpenPay.Fields.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.Payments.OpenPay.Fields.DisplayProductPageWidget"] = "Display the product page widget",
                ["Plugins.Payments.OpenPay.Fields.DisplayProductPageWidget.Hint"] = "Check to display the product page widget on a product page.",
                ["Plugins.Payments.OpenPay.Fields.DisplayProductListingWidget"] = "Display the product listing widget",
                ["Plugins.Payments.OpenPay.Fields.DisplayProductListingWidget.Hint"] = "Check to display the product listing widget in a product box (e.g. on a category page).",
                ["Plugins.Payments.OpenPay.Fields.ProductListingHideLogo"] = "Hide logo",
                ["Plugins.Payments.OpenPay.Fields.ProductListingHideLogo.Hint"] = "Check to hide the logo of the product listing widget.",
                ["Plugins.Payments.OpenPay.Fields.DisplayCartWidget"] = "Display the cart widget",
                ["Plugins.Payments.OpenPay.Fields.DisplayCartWidget.Hint"] = "Check to display the cart widget in the shopping cart.",
                ["Plugins.Payments.OpenPay.Fields.DisplayInfoBeltWidget"] = "Display the info belt widget",
                ["Plugins.Payments.OpenPay.Fields.DisplayInfoBeltWidget.Hint"] = "Check to display the info belt in the page head.",
                ["Plugins.Payments.OpenPay.Fields.DisplayLandingPageWidget"] = "Display the landing page widget",
                ["Plugins.Payments.OpenPay.Fields.DisplayLandingPageWidget.Hint"] = "Check to display the landing page link in footer.",
                ["Plugins.Payments.OpenPay.Fields.PlanTiers"] = "Plan tiers",
                ["Plugins.Payments.OpenPay.Fields.PlanTiers.Hint"] = "Enter the plan tiers in months. E.g. '2,4,6' for 2 months, 4 months and 6 months.",
                ["Plugins.Payments.OpenPay.Fields.PlanTiers.Required"] = "The plan tiers are required.",
                ["Plugins.Payments.OpenPay.Fields.ProductPageWidgetLogo"] = "Logo style",
                ["Plugins.Payments.OpenPay.Fields.ProductPageWidgetLogo.Hint"] = "Enter the logo style of the product page widget (e.g. 'grey-on-amberbg', 'grey', 'amber' or 'white').",
                ["Plugins.Payments.OpenPay.Fields.ProductPageWidgetLogoPosition"] = "Logo position",
                ["Plugins.Payments.OpenPay.Fields.ProductPageWidgetLogoPosition.Hint"] = "Enter the logo position of the product page widget (e.g. 'left' or 'right').",
                ["Plugins.Payments.OpenPay.PaymentMethodDescription"] = "Pay by Openpay",
                ["Plugins.Payments.OpenPay.InvalidCurrency"] = "The primary store currency must match the currency of the country '{0}'. You must set the primary store currency to '{1}'.",
                ["Plugins.Payments.OpenPay.DownloadOrderLimitsButton"] = "Get Min/Max limits",
                ["Plugins.Payments.OpenPay.OrderLimitsDownloaded"] = "The order limits are downloaded successfully.",
                ["Plugins.Payments.OpenPay.FailedOrderCreation"] = "Error when calling Openpay create order endpoint. Please try again or contact with store owner.",
                ["Plugins.Payments.OpenPay.IsNotConfigured"] = "Plugin isn't configured.",
                ["Plugins.Payments.OpenPay.SuccessfulPayment"] = "The payment was successful. Thanks you.",
                ["Plugins.Payments.OpenPay.InvalidPayment"] = "The payment was not processed. Please try again or contact with store owner.",
                ["Plugins.Payments.OpenPay.CartWidgetMoreInfoText"] = "Learn more",
                ["Plugins.Payments.OpenPay.ProductPageWidgetMoreInfoText"] = "Learn more",
                ["Plugins.Payments.OpenPay.Widgets"] = "Widgets",
                ["Plugins.Payments.OpenPay.OrderLimits"] = "Order limits",
                ["Plugins.Payments.OpenPay.LandingPageLinkName"] = "Openpay",
                ["Plugins.Payments.OpenPay.RedirectionTip"] = "You will be redirected to Openpay's website to complete your order.",
                ["Plugins.Payments.OpenPay.OrderLimitsDescription"] = "Click <i>Get Min/Max limits</i> button to get the currently configured Min and Max purchase price range. This is necessary in order to not display Openpay as a payment option if the order total is not within the range. You can get the order limits only 3 times on any given calendar day. Also you can configure the background task <a href=\"{0}\" target=\"_blank\">here</a> to get the order limits periodically.",
                ["Plugins.Payments.OpenPay.Instructions"] = @"
                    <p>
                        1. <a href=""https://www.openpay.co.uk/for-business/"" target=""_blank"">Apply for United Kingdom</a> or <a href=""https://www.openpay.com.au/business/"" target=""_blank"">Apply for Australia</a> Openpay Merchant Account
                        <br />2. Enter the API token provided by Openpay
                        <br />3. Choose the applicable country. Note that the primary store currency must match the currency of the country
                        <br />4. Click <i>Save</i> button
                        <br />5. Click <i>Get Min/Max limits</i> button
	                    <br />
                        <i>Note: The Openpay payment method isn't available for the products that cannot be shipped.</i>
                    </p>",
            });

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<OpenPayPaymentSettings>();

            if (_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(Defaults.SystemName);
                _settingService.SaveSetting(_widgetSettings);
            }

            //schedule task
            foreach (var task in Defaults.ScheduleTasks)
            {
                if (_scheduleTaskService.GetTaskByType(task.Type) != null)
                    _scheduleTaskService.DeleteTask(task);
            }

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Payments.OpenPay");

            base.Uninstall();
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public virtual IList<string> GetWidgetZones()
        {
            return new List<string>
            {
                PublicWidgetZones.ProductDetailsBottom,
                PublicWidgetZones.ProductBoxAddinfoMiddle,
                PublicWidgetZones.OrderSummaryContentAfter,
                PublicWidgetZones.BodyStartHtmlTagAfter,
                PublicWidgetZones.BodyEndHtmlTagBefore
            };
        }

        /// <summary>
        /// Gets a name of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component name</returns>
        public virtual string GetWidgetViewComponentName(string widgetZone)
        {
            if (widgetZone is null)
                throw new ArgumentNullException(nameof(widgetZone));

            if (widgetZone.Equals(PublicWidgetZones.ProductDetailsBottom) ||
                widgetZone.Equals(PublicWidgetZones.ProductBoxAddinfoMiddle) ||
                widgetZone.Equals(PublicWidgetZones.OrderSummaryContentAfter) ||
                widgetZone.Equals(PublicWidgetZones.BodyStartHtmlTagAfter) ||
                widgetZone.Equals(PublicWidgetZones.BodyEndHtmlTagBefore))
            {
                return Defaults.WIDGET_VIEW_COMPONENT_NAME;
            }

            return string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => true;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription => _localizationService.GetResource("Plugins.Payments.OpenPay.PaymentMethodDescription");

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => true;

        #endregion
    }
}