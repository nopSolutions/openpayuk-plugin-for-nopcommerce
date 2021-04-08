using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Plugin.Payments.OpenPay.Services;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.OpenPay.Controllers
{
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    [ValidateIpAddress]
    [AuthorizeAdmin]
    [ValidateVendor]
    public class OpenPayPaymentController : BasePaymentController
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly OpenPayApi _openPayApi;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public OpenPayPaymentController(
            CurrencySettings currencySettings,
            OpenPayApi openPayApi,
            ICountryService countryService,
            ICurrencyService currencyService,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            ISettingService settingService,
            IStoreContext storeContext
        )
        {
            _currencySettings = currencySettings;
            _openPayApi = openPayApi;
            _countryService = countryService;
            _currencyService = currencyService;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var openPayPaymentSettings = _settingService.LoadSetting<OpenPayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ActiveStoreScopeConfiguration = storeScope,
                UseSandbox = openPayPaymentSettings.UseSandbox,
                ApiToken = openPayPaymentSettings.ApiToken,
                RegionTwoLetterIsoCode = openPayPaymentSettings.RegionTwoLetterIsoCode,
                MinOrderTotal = openPayPaymentSettings.MinOrderTotal,
                MaxOrderTotal = openPayPaymentSettings.MaxOrderTotal,
                AdditionalFee = openPayPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = openPayPaymentSettings.AdditionalFeePercentage,
                DisplayPriceBreakdownInProductBox = openPayPaymentSettings.DisplayPriceBreakdownInProductBox,
                DisplayPriceBreakdownInShoppingCart = openPayPaymentSettings.DisplayPriceBreakdownInShoppingCart,
                DisplayPriceBreakdownOnProductPage = openPayPaymentSettings.DisplayPriceBreakdownOnProductPage,
                PlanTiers = openPayPaymentSettings.PlanTiers
            };

            var availableCountryCodes = Defaults.OpenPay.AvailableRegions
                .Select(region => region.TwoLetterIsoCode)
                .Distinct();

            foreach (var countryCode in availableCountryCodes)
            {
                var country = _countryService.GetCountryByTwoLetterIsoCode(countryCode);
                if (country != null)
                    model.AvailableRegions.Add(new SelectListItem(country.Name, country.TwoLetterIsoCode));
            }

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.UseSandbox, storeScope);
                model.ApiToken_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.ApiToken, storeScope);
                model.RegionTwoLetterIsoCode_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.RegionTwoLetterIsoCode, storeScope);
                model.MinOrderTotal_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.MinOrderTotal, storeScope);
                model.MaxOrderTotal_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.MaxOrderTotal, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.DisplayPriceBreakdownInProductBox_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.DisplayPriceBreakdownInProductBox, storeScope);
                model.DisplayPriceBreakdownInShoppingCart_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.DisplayPriceBreakdownInShoppingCart, storeScope);
                model.DisplayPriceBreakdownOnProductPage_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.DisplayPriceBreakdownOnProductPage, storeScope);
                model.PlanTiers_OverrideForStore = _settingService.SettingExists(openPayPaymentSettings, x => x.PlanTiers, storeScope);
            }

            return View("~/Plugins/Payments.OpenPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            // primary store currency must match the currency of the selected country
            var selectedRegion = Defaults.OpenPay.AvailableRegions
                .FirstOrDefault(region => region.TwoLetterIsoCode == model.RegionTwoLetterIsoCode);

            var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (primaryStoreCurrency.CurrencyCode != selectedRegion.CurrencyCode)
            {
                var invalidCurrencyLocale = _localizationService.GetResource("Plugins.Payments.OpenPay.InvalidCurrency");
                var invalidCurrencyMessage = string.Format(invalidCurrencyLocale, selectedRegion.TwoLetterIsoCode, selectedRegion.CurrencyCode);
                _notificationService.WarningNotification(invalidCurrencyMessage);
            }

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var openPayPaymentSettings = _settingService.LoadSetting<OpenPayPaymentSettings>(storeScope);

            //sort plan tiers
            var convertedPlanTiers = model.PlanTiers.Split(',').Select(x => int.Parse(x)).ToArray();
            Array.Sort(convertedPlanTiers);

            //save settings
            openPayPaymentSettings.UseSandbox = model.UseSandbox;
            openPayPaymentSettings.ApiToken = model.ApiToken;
            openPayPaymentSettings.RegionTwoLetterIsoCode = model.RegionTwoLetterIsoCode;
            openPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            openPayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            openPayPaymentSettings.DisplayPriceBreakdownInProductBox = model.DisplayPriceBreakdownInProductBox;
            openPayPaymentSettings.DisplayPriceBreakdownInShoppingCart = model.DisplayPriceBreakdownInShoppingCart;
            openPayPaymentSettings.DisplayPriceBreakdownOnProductPage = model.DisplayPriceBreakdownOnProductPage;
            openPayPaymentSettings.PlanTiers = string.Join(",", convertedPlanTiers);
            
            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.ApiToken, model.ApiToken_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.RegionTwoLetterIsoCode, model.RegionTwoLetterIsoCode_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.DisplayPriceBreakdownInProductBox, model.DisplayPriceBreakdownInProductBox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.DisplayPriceBreakdownInShoppingCart, model.DisplayPriceBreakdownInShoppingCart_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.DisplayPriceBreakdownOnProductPage, model.DisplayPriceBreakdownOnProductPage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.PlanTiers, model.PlanTiers_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return RedirectToAction("Configure");
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("get-order-limits")]
        public async Task<IActionResult> GetOrderLimits(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            try
            {
                //load settings for a chosen store scope
                var storeScope = _storeContext.ActiveStoreScopeConfiguration;
                var openPayPaymentSettings = _settingService.LoadSetting<OpenPayPaymentSettings>(storeScope);

                _openPayApi.ConfigureClient(openPayPaymentSettings);

                var limits = await _openPayApi.GetOrderLimitsAsync();

                openPayPaymentSettings.MinOrderTotal = limits.MinPrice / 100;
                openPayPaymentSettings.MaxOrderTotal = limits.MaxPrice / 100;

                _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.MinOrderTotal, model.MinOrderTotal_OverrideForStore, storeScope, false);
                _settingService.SaveSettingOverridablePerStore(openPayPaymentSettings, x => x.MaxOrderTotal, model.MaxOrderTotal_OverrideForStore, storeScope, false);

                //now clear settings cache
                _settingService.ClearCache();

                _notificationService.SuccessNotification(_localizationService.GetResource("Plugins.Payments.OpenPay.OrderLimitsDownloaded"));
            }
            catch (ApiException ex)
            {
                _notificationService.ErrorNotification(ex.Message);
            }

            return RedirectToAction("Configure");
        }

        #endregion
    }
}