using Nop.Core;
using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    /// <summary>
    /// Represents a schedule task to get the order limits
    /// </summary>
    public class OrderLimitsTask
    {
        #region Fields

        private readonly OpenPayApi _openPayApi;
        private readonly OpenPayService _openPayService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public OrderLimitsTask(
            OpenPayApi openPayApi,
            OpenPayService openPayService,
            ISettingService settingService,
            IStoreContext storeContext,
            ILogger logger)
        {
            _openPayApi = openPayApi;
            _openPayService = openPayService;
            _settingService = settingService;
            _storeContext = storeContext;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute task
        /// </summary>
        public void Execute()
        {
            var validationResult = _openPayService.Validate();
            if (!validationResult.IsValid)
            {
                _logger.Error($"{Defaults.SystemName}: Cannot get the order limits when background task was processed.\n{string.Join("\n", validationResult.Errors)}");
                return;
            }

            try
            {
                var limits = _openPayApi.GetOrderLimitsAsync().Result;

                var store = _storeContext.CurrentStore;
                var openPayPaymentSettings = _settingService.LoadSetting<OpenPayPaymentSettings>(store.Id);

                openPayPaymentSettings.MinOrderTotal = limits.MinPrice / 100;
                openPayPaymentSettings.MaxOrderTotal = limits.MaxPrice / 100;

                _settingService.SaveSetting(openPayPaymentSettings, store.Id);
            }
            catch (ApiException ex)
            {
                _logger.Error($"{Defaults.SystemName}: Cannot get the order limits when background task was processed.", ex);
            }
        }

        #endregion
    }
}
