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
    public class OpenPayConfigurationController : BasePaymentController
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

        public OpenPayConfigurationController(
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

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var openPayPaymentSettings = await _settingService.LoadSettingAsync<OpenPayPaymentSettings>(storeScope);

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
                DisplayProductListingWidget = openPayPaymentSettings.DisplayProductListingWidget,
                DisplayCartWidget = openPayPaymentSettings.DisplayCartWidget,
                CartWidgetLogo = openPayPaymentSettings.CartWidgetLogo,
                DisplayProductPageWidget = openPayPaymentSettings.DisplayProductPageWidget,
                DisplayInfoBeltWidget = openPayPaymentSettings.DisplayInfoBeltWidget,
                DisplayLandingPageWidget = openPayPaymentSettings.DisplayLandingPageWidget,
                PlanTiers = openPayPaymentSettings.PlanTiers,
                InfoBeltWidgetColor = openPayPaymentSettings.InfoBeltWidgetColor,
                ProductListingWidgetLogo = openPayPaymentSettings.ProductListingWidgetLogo,
                ProductListingHideLogo = openPayPaymentSettings.ProductListingHideLogo,
                ProductPageWidgetLogo = openPayPaymentSettings.ProductPageWidgetLogo,
                ProductPageWidgetLogoPosition = openPayPaymentSettings.ProductPageWidgetLogoPosition
            };

            var availableCountryCodes = Defaults.OpenPay.AvailableRegions
                .Select(region => region.TwoLetterIsoCode)
                .Distinct();

            foreach (var countryCode in availableCountryCodes)
            {
                var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(countryCode);
                if (country != null)
                    model.AvailableRegions.Add(new SelectListItem(country.Name, country.TwoLetterIsoCode));
            }

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.UseSandbox, storeScope);
                model.ApiToken_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.ApiToken, storeScope);
                model.RegionTwoLetterIsoCode_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.RegionTwoLetterIsoCode, storeScope);
                model.MinOrderTotal_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.MinOrderTotal, storeScope);
                model.MaxOrderTotal_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.MaxOrderTotal, storeScope);
                model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.DisplayProductListingWidget_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.DisplayProductListingWidget, storeScope);
                model.DisplayCartWidget_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.DisplayCartWidget, storeScope);
                model.CartWidgetLogo_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.CartWidgetLogo, storeScope);
                model.DisplayProductPageWidget_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.DisplayProductPageWidget, storeScope);
                model.DisplayInfoBeltWidget_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.DisplayInfoBeltWidget, storeScope);
                model.DisplayLandingPageWidget_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.DisplayLandingPageWidget, storeScope);
                model.PlanTiers_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.PlanTiers, storeScope);
                model.InfoBeltWidgetColor_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.InfoBeltWidgetColor, storeScope);
                model.ProductListingWidgetLogo_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.PlanTiers, storeScope);
                model.ProductListingHideLogo_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.PlanTiers, storeScope);
                model.ProductPageWidgetLogo_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.ProductPageWidgetLogo, storeScope);
                model.ProductPageWidgetLogoPosition_OverrideForStore = await _settingService.SettingExistsAsync(openPayPaymentSettings, x => x.ProductPageWidgetLogoPosition, storeScope);
            }

            return View("~/Plugins/Payments.OpenPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            // primary store currency must match the currency of the selected country
            var selectedRegion = Defaults.OpenPay.AvailableRegions
                .FirstOrDefault(region => region.TwoLetterIsoCode == model.RegionTwoLetterIsoCode);

            var primaryStoreCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            if (primaryStoreCurrency.CurrencyCode != selectedRegion.CurrencyCode)
            {
                var invalidCurrencyLocale = await _localizationService.GetResourceAsync("Plugins.Payments.OpenPay.InvalidCurrency");
                var invalidCurrencyMessage = string.Format(invalidCurrencyLocale, selectedRegion.TwoLetterIsoCode, selectedRegion.CurrencyCode);
                _notificationService.WarningNotification(invalidCurrencyMessage);
            }

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var openPayPaymentSettings = await _settingService.LoadSettingAsync<OpenPayPaymentSettings>(storeScope);

            //sort plan tiers
            var convertedPlanTiers = model.PlanTiers.Split(',').Select(x => int.Parse(x)).ToArray();
            Array.Sort(convertedPlanTiers);

            //save settings
            openPayPaymentSettings.UseSandbox = model.UseSandbox;
            openPayPaymentSettings.ApiToken = model.ApiToken;
            openPayPaymentSettings.RegionTwoLetterIsoCode = model.RegionTwoLetterIsoCode;
            openPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            openPayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            openPayPaymentSettings.DisplayProductListingWidget = model.DisplayProductListingWidget;
            openPayPaymentSettings.DisplayCartWidget = model.DisplayCartWidget;
            openPayPaymentSettings.CartWidgetLogo = model.CartWidgetLogo;
            openPayPaymentSettings.DisplayProductPageWidget = model.DisplayProductPageWidget;
            openPayPaymentSettings.DisplayInfoBeltWidget = model.DisplayInfoBeltWidget;
            openPayPaymentSettings.DisplayLandingPageWidget = model.DisplayLandingPageWidget;
            openPayPaymentSettings.PlanTiers = string.Join(",", convertedPlanTiers);
            openPayPaymentSettings.InfoBeltWidgetColor = model.InfoBeltWidgetColor;
            openPayPaymentSettings.ProductListingWidgetLogo = model.ProductListingWidgetLogo;
            openPayPaymentSettings.ProductListingHideLogo = model.ProductListingHideLogo;
            openPayPaymentSettings.ProductPageWidgetLogo = model.ProductPageWidgetLogo;
            openPayPaymentSettings.ProductPageWidgetLogoPosition = model.ProductPageWidgetLogoPosition;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.ApiToken, model.ApiToken_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.RegionTwoLetterIsoCode, model.RegionTwoLetterIsoCode_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.DisplayProductListingWidget, model.DisplayProductListingWidget_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.DisplayCartWidget, model.DisplayCartWidget_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.CartWidgetLogo, model.CartWidgetLogo_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.DisplayProductPageWidget, model.DisplayProductPageWidget_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.DisplayInfoBeltWidget, model.DisplayInfoBeltWidget_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.DisplayLandingPageWidget, model.DisplayLandingPageWidget_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.PlanTiers, model.PlanTiers_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.InfoBeltWidgetColor, model.InfoBeltWidgetColor_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.ProductListingWidgetLogo, model.ProductListingWidgetLogo_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.ProductListingHideLogo, model.ProductListingHideLogo_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.ProductPageWidgetLogo, model.ProductPageWidgetLogo_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.ProductPageWidgetLogoPosition, model.ProductPageWidgetLogoPosition_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return RedirectToAction("Configure");
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("get-order-limits")]
        public async Task<IActionResult> GetOrderLimits(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return await Configure();

            try
            {
                //load settings for a chosen store scope
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var openPayPaymentSettings = await _settingService.LoadSettingAsync<OpenPayPaymentSettings>(storeScope);

                _openPayApi.ConfigureClient(openPayPaymentSettings);

                var limits = await _openPayApi.GetOrderLimitsAsync();

                openPayPaymentSettings.MinOrderTotal = limits.MinPrice / 100;
                openPayPaymentSettings.MaxOrderTotal = limits.MaxPrice / 100;

                await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.MinOrderTotal, model.MinOrderTotal_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(openPayPaymentSettings, x => x.MaxOrderTotal, model.MaxOrderTotal_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Payments.OpenPay.OrderLimitsDownloaded"));
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