using System;
using System.Threading.Tasks;
using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Stores;
using Scheduling = Nop.Services.Tasks;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    /// <summary>
    /// Represents a schedule task to get the order limits
    /// </summary>
    public class OrderLimitsTask : Scheduling.IScheduleTask
    {
        #region Fields

        private readonly OpenPayApi _openPayApi;
        private readonly OpenPayService _openPayService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public OrderLimitsTask(
            OpenPayApi openPayApi,
            OpenPayService openPayService,
            ISettingService settingService,
            IStoreService storeService,
            ILogger logger)
        {
            _openPayApi = openPayApi;
            _openPayService = openPayService;
            _settingService = settingService;
            _storeService = storeService;
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
                    await _logger.ErrorAsync($"{Defaults.SystemName}: Cannot get the order limits for the store '{store.Name}' when background task was processed.{Environment.NewLine}{string.Join(Environment.NewLine, validationResult.Errors)}");
                    continue;
                }

                var openPayPaymentSettings = await _settingService.LoadSettingAsync<OpenPayPaymentSettings>(store.Id);

                _openPayApi.ConfigureClient(openPayPaymentSettings);

                try
                {
                    var limits = await _openPayApi.GetOrderLimitsAsync();

                    openPayPaymentSettings.MinOrderTotal = limits.MinPrice / 100;
                    openPayPaymentSettings.MaxOrderTotal = limits.MaxPrice / 100;

                    await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.MinOrderTotal, stores.Count > 1, store.Id, false);
                    await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.MaxOrderTotal, stores.Count > 1, store.Id, false);

                    await _settingService.ClearCacheAsync();
                }
                catch (ApiException ex)
                {
                    await _logger.ErrorAsync($"{Defaults.SystemName}: Cannot get the order limits for the store '{store.Name}' when background task was processed.", ex);
                }
            }
        }

        #endregion
    }
}
