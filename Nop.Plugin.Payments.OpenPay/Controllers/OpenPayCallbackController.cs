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
    public class OpenPayCallbackController : BasePaymentController
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

        public OpenPayCallbackController(
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
            if (!(_paymentPluginManager.LoadPluginBySystemName(Defaults.SystemName) is OpenPayPaymentProcessor processor) || !_paymentPluginManager.IsPluginActive(processor))
                return ProduceErrorResponse();

            if (!orderId.HasValue || string.IsNullOrEmpty(planId) || string.IsNullOrEmpty(status))
                return ProduceErrorResponse();

            var order = _orderService.GetOrderById(orderId.Value);
            if (order == null || order.Deleted)
                return ProduceErrorResponse(null, $"Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' not found or was deleted.");

            var openPayOrder = await _openPayApi.GetOrderStatusByIdAsync(planId);
            if (openPayOrder == null)
                return ProduceErrorResponse(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. Cannot get the OpenPay order by id '{planId}'.");

            if (!openPayOrder.PlanStatus.Equals(status, StringComparison.InvariantCultureIgnoreCase))
                return ProduceErrorResponse(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. The OpenPay plan status '{status}' is invalid.");

            if (!openPayOrder.OrderStatus.Equals("Pending", StringComparison.InvariantCultureIgnoreCase))
                return ProduceErrorResponse(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. The OpenPay order status '{openPayOrder.OrderStatus}' is invalid.");

            if (!openPayOrder.PlanStatus.Equals("Lodged", StringComparison.InvariantCultureIgnoreCase))
                return ProduceErrorResponse(order.Id, "Invalid processing payment after the order successfully placed on OpenPay. The OpenPay plan status should be 'Lodged'.");

            if (!_orderProcessingService.CanMarkOrderAsPaid(order))
                return ProduceErrorResponse(order.Id, $"Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' already marked as paid.");

            var result = _openPayService.CaptureOrder(order);
            if (string.IsNullOrEmpty(result.OrderId))
                return ProduceErrorResponse(order.Id, string.Join("\n", result.Errors));

            order.CaptureTransactionId = result.OrderId;
            _orderProcessingService.MarkOrderAsPaid(order);

            _notificationService.SuccessNotification(
                _localizationService.GetResource("Plugins.Payments.OpenPay.SuccessfulPayment"));

            return RedirectToAction("Completed", "Checkout", new { orderId = order.Id });
        }

        #endregion

        #region Utilities

        private IActionResult ProduceErrorResponse(int? orderId = null, string errorMessage = null)
        {
            if (!string.IsNullOrEmpty(errorMessage) && _openPayPaymentSettings.LogCallbackErrors)
                _logger.Error($"{Defaults.SystemName}: {errorMessage}");

            _notificationService.ErrorNotification(
                _localizationService.GetResource("Plugins.Payments.OpenPay.InvalidPayment"));

            return orderId.HasValue
                ? RedirectToAction("Details", "Order", new { orderId })
                : RedirectToAction("Index", "Home");
        }

        #endregion
    }
}
