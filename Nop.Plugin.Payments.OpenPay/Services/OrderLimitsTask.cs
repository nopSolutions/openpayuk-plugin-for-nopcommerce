using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Stores;
using Nop.Services.Tasks;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    /// <summary>
    /// Represents a schedule task to get the order limits
    /// </summary>
    public class OrderLimitsTask : IScheduleTask
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
        public void Execute()
        {
            var stores = _storeService.GetAllStores();
            foreach (var store in stores)
            {
                var validationResult = _openPayService.Validate(store.Id);
                if (!validationResult.IsValid)
                {
                    _logger.Error($"{Defaults.SystemName}: Cannot get the order limits for the store '{store.Name}' when background task was processed.\n{string.Join("\n", validationResult.Errors)}");
                    continue;
                }

                var openPayPaymentSettings = _settingService.LoadSetting<OpenPayPaymentSettings>(store.Id);

                _openPayApi.ConfigureClient(openPayPaymentSettings);

                try
                {
                    var limits = _openPayApi.GetOrderLimitsAsync().Result;

                    openPayPaymentSettings.MinOrderTotal = limits.MinPrice / 100;
                    openPayPaymentSettings.MaxOrderTotal = limits.MaxPrice / 100;

                    _settingService.SaveSetting(openPayPaymentSettings, store.Id);
                }
                catch (ApiException ex)
                {
                    _logger.Error($"{Defaults.SystemName}: Cannot get the order limits for the store '{store.Name}' when background task was processed.", ex);
                }
            }
        }

        #endregion
    }
}
