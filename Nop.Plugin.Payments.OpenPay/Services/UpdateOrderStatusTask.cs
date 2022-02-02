using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Scheduling = Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    /// <summary>
    /// Represents a schedule task to update the order status
    /// </summary>
    public class UpdateOrderStatusTask : Scheduling.IScheduleTask
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
        /// <returns>The <see cref="Task"/></returns>
        public async Task ExecuteAsync()
        {
            var stores = await _storeService.GetAllStoresAsync();
            foreach (var store in stores)
            {
                var validationResult = await _openPayService.ValidateAsync(store.Id);
                if (!validationResult.IsValid)
                {
                    await _logger.ErrorAsync($"{Defaults.SystemName}: Cannot update the status of the orders in the store '{store.Name}' when background task was processed.{Environment.NewLine}{string.Join(Environment.NewLine, validationResult.Errors)}");
                    continue;
                }

                // get all non-paid orders including previous month 
                var orders = (await _orderService.SearchOrdersAsync(
                    storeId: store.Id,
                    createdFromUtc: DateTime.UtcNow.AddMonths(-1),
                    psIds: new List<int>
                    {
                        (int)PaymentStatus.Pending,
                        (int)PaymentStatus.Authorized
                    }))?.Where(o => o.PaymentMethodSystemName == Defaults.SystemName);
                
                if (orders?.Any() == true)
                {
                    var openPayPaymentSettings = await _settingService.LoadSettingAsync<OpenPayPaymentSettings>(store.Id);

                    _openPayApi.ConfigureClient(openPayPaymentSettings);

                    foreach (var order in orders)
                    {
                        var result = await _openPayService.CaptureOrderAsync(order);
                        if (string.IsNullOrEmpty(result.OrderId))
                            await _logger.ErrorAsync($"{Defaults.SystemName}: Cannot update the status of the order '{order.CustomOrderNumber}' in the store '{store.Name}' when background task was processed.{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors)}");
                        else if (_orderProcessingService.CanMarkOrderAsPaid(order))
                        {
                            order.CaptureTransactionId = result.OrderId;
                            await _orderProcessingService.MarkOrderAsPaidAsync(order);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
