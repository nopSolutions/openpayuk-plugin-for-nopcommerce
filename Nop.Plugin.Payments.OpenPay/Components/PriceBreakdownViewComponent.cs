using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Plugin.Payments.OpenPay.Services;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Payments.OpenPay.Components
{
    /// <summary>
    /// Represents the view component to display the price breakdown in public store
    /// </summary>
    [ViewComponent(Name = Defaults.PRICE_BREAKDOWN_VIEW_COMPONENT_NAME)]
    public class PriceBreakdownViewComponent : NopViewComponent
    {
        #region Fields

        private readonly OpenPayService _openPayService;
        private readonly OpenPayPaymentSettings _openPayPaymentSettings;
        private readonly ICurrencyService _currencyService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public PriceBreakdownViewComponent(
            OpenPayService openPayService,
            OpenPayPaymentSettings openPayPaymentSettings,
            ICurrencyService currencyService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _openPayService = openPayService;
            _openPayPaymentSettings = openPayPaymentSettings;
            _currencyService = currencyService;
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

            var region = Defaults.OpenPay.AvailableRegions.FirstOrDefault(
                region => region.IsSandbox == _openPayPaymentSettings.UseSandbox && region.TwoLetterIsoCode == _openPayPaymentSettings.RegionTwoLetterIsoCode);

            var model = new PriceBreakdownModel
            {
                WidgetCode = region.WidgetCode,
                RegionCode = region.TwoLetterIsoCode,
                CurrencyCode = _workContext.WorkingCurrency.CurrencyCode,
                PlanTiers = _openPayPaymentSettings.PlanTiers.Split(',').Select(x => int.Parse(x)).ToArray(),
                MinEligibleAmount = ToWorkingPrice(_openPayPaymentSettings.MinOrderTotal / 100),
                MaxEligibleAmount = ToWorkingPrice(_openPayPaymentSettings.MaxOrderTotal / 100),
                Type = "Online"
            };

            if (widgetZone == PublicWidgetZones.ProductDetailsBottom && additionalData is ProductDetailsModel productDetailsModel)
            {
                if (!_openPayPaymentSettings.DisplayPriceBreakdownOnProductPage)
                    return Content(string.Empty);

                model.Amount = ToWorkingPrice(productDetailsModel.ProductPrice.PriceValue);
                return View("~/Plugins/Payments.OpenPay/Views/PriceBreakdown/ProductPage.cshtml", model);
            }
            
            if (widgetZone == PublicWidgetZones.ProductBoxAddinfoMiddle && additionalData is ProductOverviewModel productOverviewModel)
            {
                if (!_openPayPaymentSettings.DisplayPriceBreakdownInProductBox)
                    return Content(string.Empty);

                model.Amount = ToWorkingPrice(productOverviewModel.ProductPrice.PriceValue);
                return View("~/Plugins/Payments.OpenPay/Views/PriceBreakdown/ProductListing.cshtml", model);
            }

            if (widgetZone == PublicWidgetZones.OrderSummaryContentAfter)
            {
                if (!_openPayPaymentSettings.DisplayPriceBreakdownInShoppingCart)
                    return Content(string.Empty);

                var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
                if (routeName != Defaults.ShoppingCartRouteName)
                    return Content(string.Empty);

                var customer = _workContext.CurrentCustomer;
                var store = _storeContext.CurrentStore;
                var cart = _shoppingCartService.GetShoppingCart(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (cart == null || !cart.Any())
                    return Content(string.Empty);

                var cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);
                if (!cartTotal.HasValue)
                    return Content(string.Empty);

                model.Amount = ToWorkingPrice(cartTotal.Value);
                return View("~/Plugins/Payments.OpenPay/Views/PriceBreakdown/Cart.cshtml", model);
            }

            return Content(string.Empty);
        }

        #endregion

        #region Utilities

        private decimal ToWorkingPrice(decimal price)
        {
            return _currencyService.ConvertFromPrimaryStoreCurrency(price, _workContext.WorkingCurrency);
        }

        #endregion
    }
}
