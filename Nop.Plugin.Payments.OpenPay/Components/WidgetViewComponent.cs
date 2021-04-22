using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Plugin.Payments.OpenPay.Services;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Payments.OpenPay.Components
{
    /// <summary>
    /// Represents the view component to display the OpenPay widget in public store
    /// </summary>
    [ViewComponent(Name = Defaults.WIDGET_VIEW_COMPONENT_NAME)]
    public class WidgetViewComponent : NopViewComponent
    {
        #region Fields

        private readonly OpenPayService _openPayService;
        private readonly OpenPayPaymentSettings _openPayPaymentSettings;
        private readonly ICurrencyService _currencyService;
        private readonly IProductService _productService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public WidgetViewComponent(
            OpenPayService openPayService,
            OpenPayPaymentSettings openPayPaymentSettings,
            ICurrencyService currencyService,
            IProductService productService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _openPayService = openPayService;
            _openPayPaymentSettings = openPayPaymentSettings;
            _currencyService = currencyService;
            _productService = productService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invokes the view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>The view component result</returns>
        public IViewComponentResult Invoke(string widgetZone, object additionalData)
        {
            if (!_openPayService.CanDisplayWidget())
                return Content(string.Empty);

            if (widgetZone == PublicWidgetZones.BodyEndHtmlTagBefore)
            {
                if (!_openPayPaymentSettings.DisplayLandingPageWidget)
                    return Content(string.Empty);

                return View("~/Plugins/Payments.OpenPay/Views/Widget/LandingPageLink.cshtml");
            }

            var region = Defaults.OpenPay.AvailableRegions.FirstOrDefault(
                region => region.IsSandbox == _openPayPaymentSettings.UseSandbox && region.TwoLetterIsoCode == _openPayPaymentSettings.RegionTwoLetterIsoCode);

            var model = new WidgetModel
            {
                WidgetCode = region.WidgetCode,
                RegionCode = region.TwoLetterIsoCode,
                CurrencyCode = _workContext.WorkingCurrency.CurrencyCode,
                PlanTiers = _openPayPaymentSettings.PlanTiers.Split(',').Select(x => int.Parse(x)).ToArray(),
                MinEligibleAmount = ToWorkingCurrency(_openPayPaymentSettings.MinOrderTotal),
                MaxEligibleAmount = ToWorkingCurrency(_openPayPaymentSettings.MaxOrderTotal),
                Type = "Online"
            };

            if (widgetZone == PublicWidgetZones.ProductDetailsBottom && additionalData is ProductDetailsModel productDetailsModel)
            {
                if (!_openPayPaymentSettings.DisplayProductPageWidget)
                    return Content(string.Empty);

                var product = _productService.GetProductById(productDetailsModel.Id);
                if (product == null || product.Deleted || product.IsDownload || !product.IsShipEnabled)
                    return Content(string.Empty);

                model.Logo = _openPayPaymentSettings.ProductPageWidgetLogo;
                model.LogoPosition = _openPayPaymentSettings.ProductPageWidgetLogoPosition;
                model.Amount = productDetailsModel.ProductPrice.PriceValue;

                return View("~/Plugins/Payments.OpenPay/Views/Widget/ProductPage.cshtml", model);
            }
            
            if (widgetZone == PublicWidgetZones.ProductBoxAddinfoMiddle && additionalData is ProductOverviewModel productOverviewModel)
            {
                if (!_openPayPaymentSettings.DisplayProductListingWidget)
                    return Content(string.Empty);

                var product = _productService.GetProductById(productOverviewModel.Id);
                if (product == null || product.Deleted || product.IsDownload || !product.IsShipEnabled)
                    return Content(string.Empty);

                model.Logo = _openPayPaymentSettings.ProductListingWidgetLogo;
                model.HideLogo = _openPayPaymentSettings.ProductListingHideLogo;
                model.Amount = productOverviewModel.ProductPrice.PriceValue;

                return View("~/Plugins/Payments.OpenPay/Views/Widget/ProductListing.cshtml", model);
            }

            if (widgetZone == PublicWidgetZones.OrderSummaryContentAfter)
            {
                if (!_openPayPaymentSettings.DisplayCartWidget)
                    return Content(string.Empty);

                var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
                if (routeName != Defaults.ShoppingCartRouteName)
                    return Content(string.Empty);

                var customer = _workContext.CurrentCustomer;
                var store = _storeContext.CurrentStore;
                var cart = _shoppingCartService.GetShoppingCart(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (cart == null || !cart.Any())
                    return Content(string.Empty);

                if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
                    return Content(string.Empty);

                var cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);
                if (!cartTotal.HasValue)
                {
                    _orderTotalCalculationService.GetShoppingCartSubTotal(cart, true, out _, out _, out _, out var subTotalWithDiscount);
                    cartTotal = subTotalWithDiscount;
                }

                model.Logo = _openPayPaymentSettings.CartWidgetLogo;
                model.Amount = ToWorkingCurrency(cartTotal.Value);

                return View("~/Plugins/Payments.OpenPay/Views/Widget/Cart.cshtml", model);
            }

            if (widgetZone == PublicWidgetZones.BodyStartHtmlTagAfter)
            {
                if (!_openPayPaymentSettings.DisplayInfoBeltWidget)
                    return Content(string.Empty);

                model.Color = _openPayPaymentSettings.InfoBeltWidgetColor;

                return View("~/Plugins/Payments.OpenPay/Views/Widget/InfoBelt.cshtml", model);
            }

            if (widgetZone == "OpenPayLandingPage")
            {
                if (!_openPayPaymentSettings.DisplayLandingPageWidget)
                    return Content(string.Empty);

                return View("~/Plugins/Payments.OpenPay/Views/Widget/LandingPage.cshtml", model);
            }

            return Content(string.Empty);
        }

        #endregion

        #region Utilities

        private decimal ToWorkingCurrency(decimal price)
        {
            return _currencyService.ConvertFromPrimaryStoreCurrency(price, _workContext.WorkingCurrency);
        }

        #endregion
    }
}
