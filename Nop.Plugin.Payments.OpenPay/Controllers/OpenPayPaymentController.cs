using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.OpenPay.Services;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.OpenPay.Controllers
{
    public class OpenPayPaymentController : BasePaymentController
    {
        #region Fields

        private readonly OpenPayApi _openPayApi;
        private readonly OpenPayService _openPayService;
        private readonly OpenPayPaymentSettings _openPayPaymentSettings;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public OpenPayPaymentController(
            OpenPayApi openPayApi,
            OpenPayService openPayService,
            OpenPayPaymentSettings openPayPaymentSettings,
            IPaymentPluginManager paymentPluginManager,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            ILogger logger)
        {
            _openPayApi = openPayApi;
            _openPayService = openPayService;
            _openPayPaymentSettings = openPayPaymentSettings;
            _paymentPluginManager = paymentPluginManager;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _logger = logger;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> SuccessfulPayment(string status, string planId, int? orderId)
        {
            if (await _paymentPluginManager.LoadPluginBySystemNameAsync(Defaults.SystemName) is not OpenPayPaymentProcessor processor || !_paymentPluginManager.IsPluginActive(processor))
                return await ProduceErrorResponseAsync();

            if (!orderId.HasValue || string.IsNullOrEmpty(planId) || string.IsNullOrEmpty(status))
                return await ProduceErrorResponseAsync();

            var order = await _orderService.GetOrderByIdAsync(orderId.Value);
            if (order == null || order.Deleted)
                return await ProduceErrorResponseAsync(null, $"Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' not found or was deleted.");

            var openPayOrder = await _openPayApi.GetOrderStatusByIdAsync(planId);
            if (openPayOrder == null)
                return await ProduceErrorResponseAsync(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. Cannot get the OpenPay order by id '{planId}'.");

            if (!openPayOrder.PlanStatus.Equals(status, StringComparison.InvariantCultureIgnoreCase))
                return await ProduceErrorResponseAsync(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. The OpenPay plan status '{status}' is invalid.");

            if (!openPayOrder.OrderStatus.Equals("Pending", StringComparison.InvariantCultureIgnoreCase))
                return await ProduceErrorResponseAsync(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. The OpenPay order status '{openPayOrder.OrderStatus}' is invalid.");

            if (!openPayOrder.PlanStatus.Equals("Lodged", StringComparison.InvariantCultureIgnoreCase))
                return await ProduceErrorResponseAsync(order.Id, "Invalid processing payment after the order successfully placed on OpenPay. The OpenPay plan status should be 'Lodged'.");

            if (!_orderProcessingService.CanMarkOrderAsPaid(order))
                return await ProduceErrorResponseAsync(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' already marked as paid.");

            var result = await _openPayService.CaptureOrderAsync(order);
            if (string.IsNullOrEmpty(result.OrderId))
                return await ProduceErrorResponseAsync(order.Id, string.Join(Environment.NewLine, result.Errors));

            order.CaptureTransactionId = result.OrderId;

            if (_orderProcessingService.CanMarkOrderAsPaid(order))
                await _orderProcessingService.MarkOrderAsPaidAsync(order);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Plugins.Payments.OpenPay.SuccessfulPayment"));

            return RedirectToAction("Completed", "Checkout", new { orderId = order.Id });
        }

        public async Task<IActionResult> LandingPage()
        {
            if (await _paymentPluginManager.LoadPluginBySystemNameAsync(Defaults.SystemName) is not OpenPayPaymentProcessor processor || !_paymentPluginManager.IsPluginActive(processor))
                return RedirectToAction("Index", "Home");

            if (!_openPayPaymentSettings.DisplayLandingPageWidget)
                return RedirectToAction("Index", "Home");

            return ViewComponent(Defaults.WIDGET_VIEW_COMPONENT_NAME, new { widgetZone = "OpenPayLandingPage" });
        }

        #endregion

        #region Utilities

        private async Task<IActionResult> ProduceErrorResponseAsync(int? orderId = null, string errorMessage = null)
        {
            if (!string.IsNullOrEmpty(errorMessage) && _openPayPaymentSettings.LogCallbackErrors)
                await _logger.ErrorAsync($"{Defaults.SystemName}: {errorMessage}");

            _notificationService.ErrorNotification(
                await _localizationService.GetResourceAsync("Plugins.Payments.OpenPay.InvalidPayment"));

            return orderId.HasValue
                ? RedirectToAction("Details", "Order", new { orderId })
                : RedirectToAction("Index", "Home");
        }

        #endregion
    }
}
