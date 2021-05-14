using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Services.Tasks;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    /// <summary>
    /// Represents a schedule task to update the order status
    /// </summary>
    public class UpdateOrderStatusTask : IScheduleTask
    {
        #region Fields

        private readonly OpenPayApi _openPayApi;
        private readonly OpenPayService _openPayService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public UpdateOrderStatusTask(
            OpenPayApi openPayApi,
            OpenPayService openPayService,
            ISettingService settingService,
            IStoreService storeService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILogger logger)
        {
            _openPayApi = openPayApi;
            _openPayService = openPayService;
            _settingService = settingService;
            _storeService = storeService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute task
        /// </summary>
        public void Execute()
        {
            var stores = _storeService.GetAllStores();
            foreach (var store in stores)
            {
                var validationResult = _openPayService.Validate(store.Id);
                if (!validationResult.IsValid)
                {
                    _logger.Error($"{Defaults.SystemName}: Cannot update the status of the orders in the store '{store.Name}' when background task was processed.\n{string.Join("\n", validationResult.Errors)}");
                    continue;
                }

                // get all non-paid orders including previous month 
                var orders = _orderService.SearchOrders(
                    storeId: store.Id,
                    createdFromUtc: DateTime.UtcNow.AddMonths(-1),
                    psIds: new List<int>
                    {
                        (int)PaymentStatus.Pending,
                        (int)PaymentStatus.Authorized
                    })?.Where(o => o.PaymentMethodSystemName == Defaults.SystemName);
                
                if (orders?.Any() == true)
                {
                    var openPayPaymentSettings = _settingService.LoadSetting<OpenPayPaymentSettings>(store.Id);

                    _openPayApi.ConfigureClient(openPayPaymentSettings);

                    foreach (var order in orders)
                    {
                        var result = _openPayService.CaptureOrderAsync(order).Result;
                        if (string.IsNullOrEmpty(result.OrderId))
                            _logger.Error($"{Defaults.SystemName}: Cannot update the status of the order '{order.CustomOrderNumber}' in the store '{store.Name}' when background task was processed.\n{string.Join("\n", result.Errors)}");
                        else if (_orderProcessingService.CanMarkOrderAsPaid(order))
                        {
                            order.CaptureTransactionId = result.OrderId;
                            _orderProcessingService.MarkOrderAsPaid(order);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
