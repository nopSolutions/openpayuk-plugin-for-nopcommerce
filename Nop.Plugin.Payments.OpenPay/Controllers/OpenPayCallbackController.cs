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
            IPaymentPluginManager paymentPluginManager,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            ILogger logger)
        {
            _openPayApi = openPayApi;
            _openPayService = openPayService;
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
                return RedirectToAction("Index", "Home");

            if (!orderId.HasValue || string.IsNullOrEmpty(planId) || string.IsNullOrEmpty(status))
                return RedirectToAction("Index", "Home");

            var order = _orderService.GetOrderById(orderId.Value);
            if (order == null || order.Deleted)
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' was deleted.");
                return RedirectToAction("Index", "Home");
            }

            var openPayOrder = await _openPayApi.GetOrderStatusByIdAsync(planId);
            if (openPayOrder == null)
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. Cannot get the OpenPay order by id '{planId}'.");
                return RedirectToAction("Index", "Home");
            };

            if (!openPayOrder.PlanStatus.Equals(status, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. The OpenPay plan status '{status}' is invalid.");
                return RedirectToAction("Index", "Home");
            };

            if (!openPayOrder.OrderStatus.Equals("Pending", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. The OpenPay order status '{openPayOrder.OrderStatus}' is invalid.");
                return RedirectToAction("Index", "Home");
            };

            if (!openPayOrder.PlanStatus.Equals("Lodged", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. The OpenPay plan status should be 'Lodged'.");
                return RedirectToAction("Index", "Home");
            }

            if (!_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' already marked as paid.");
                return RedirectToAction("Index", "Home");
            }

            var result = _openPayService.CaptureOrder(order);
            if (!string.IsNullOrEmpty(result.OrderId))
            {
                order.CaptureTransactionId = result.OrderId;
                _orderProcessingService.MarkOrderAsPaid(order);

                _notificationService.SuccessNotification(
                    _localizationService.GetResource("Plugins.Payments.OpenPay.SuccessfulPayment"));
            }
            else
            {
                _logger.Error($"{Defaults.SystemName}: {string.Join("\n", result.Errors)}");

                _notificationService.ErrorNotification(
                    _localizationService.GetResource("Plugins.Payments.OpenPay.InvalidPayment"));
            }

            return RedirectToAction("Details", "Order", new { orderId = order.Id });
        }

        #endregion
    }
}
