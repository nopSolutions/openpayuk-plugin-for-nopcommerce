using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.OpenPay.Services;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.OpenPay.Controllers
{
    public class OpenPayWebhookController : Controller
    {
        #region Fields

        private readonly OpenPayApi _openPayApi;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public OpenPayWebhookController(
            OpenPayApi openPayApi,
            IPaymentPluginManager paymentPluginManager,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            ILogger logger)
        {
            _openPayApi = openPayApi;
            _paymentPluginManager = paymentPluginManager;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _logger = logger;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> SuccessfulPaymentWebhook(string status, string planId, int? orderId)
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

            if (!_orderProcessingService.CanMarkOrderAsAuthorized(order))
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' cannot be authorized.");
                return RedirectToAction("Index", "Home");
            }

            order.AuthorizationTransactionId = openPayOrder.OrderId;
            _orderProcessingService.MarkAsAuthorized(order);
            
            if (!_orderProcessingService.CanCapture(order))
            {
                _logger.Error($"{Defaults.SystemName}: Invalid processing payment after the order successfully placed on OpenPay. The order '{order.CustomOrderNumber}' cannot be captured.");
                return RedirectToAction("Index", "Home");
            }

            var errors = _orderProcessingService.Capture(order);
            if (errors.Count == 0)
            {
                _notificationService.SuccessNotification(
                    _localizationService.GetResource("Plugins.Payments.OpenPay.SuccessfulPayment"));
            }
            else
            {
                _notificationService.ErrorNotification(
                    _localizationService.GetResource("Plugins.Payments.OpenPay.InvalidPayment"));
            }

            return RedirectToAction("Details", "Order", new { orderId = order.Id });
        }

        #endregion
    }
}
