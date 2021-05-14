using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    public class OpenPayService
    {
        #region Fields

        private readonly CustomerSettings _customerSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly OpenPayApi _openPayApi;
        private readonly OpenPayPaymentSettings _openPayPaymentSettings;
        private readonly IAddressService _addressService;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderService _orderService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IProductService _productService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ISettingService _settingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public OpenPayService(
            CustomerSettings customerSettings,
            CurrencySettings currencySettings,
            OpenPayApi openPayApi,
            OpenPayPaymentSettings openPayPaymentSettings,
            IAddressService addressService,
            IActionContextAccessor actionContextAccessor,
            ICustomerService customerService,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IProductService productService,
            IProductAttributeFormatter productAttributeFormatter,
            IStateProvinceService stateProvinceService,
            ISettingService settingService,
            IShoppingCartService shoppingCartService,
            ILocalizationService localizationService,
            IUrlHelperFactory urlHelperFactory,
            IWebHelper webHelper)
        {
            _customerSettings = customerSettings;
            _currencySettings = currencySettings;
            _openPayApi = openPayApi;
            _openPayPaymentSettings = openPayPaymentSettings;
            _addressService = addressService;
            _actionContextAccessor = actionContextAccessor;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _orderService = orderService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _productService = productService;
            _productAttributeFormatter = productAttributeFormatter;
            _stateProvinceService = stateProvinceService;
            _settingService = settingService;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _localizationService = localizationService;
            _urlHelperFactory = urlHelperFactory;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the errors as <see cref="IList{string}"/> if plugin isn't configured; otherwise empty
        /// </summary>
        /// <param name="storeId">The store id, pass null to use id of the current store</param>
        /// <returns>The errors as <see cref="IList{string}"/> if plugin isn't configured; otherwise empty</returns>
        public virtual (bool IsValid, IList<string> Errors) Validate(int? storeId = null)
        {
            OpenPayPaymentSettings openPayPaymentSettings = null;

            if (!storeId.HasValue)
                openPayPaymentSettings = _openPayPaymentSettings;
            else
            {
                // load settings for specified store
                openPayPaymentSettings = _settingService.LoadSetting<OpenPayPaymentSettings>(storeId.Value);
            }

            var errors = new List<string>();

            // resolve validator here to exclude warnings after installation process
            var validator = EngineContext.Current.Resolve<IValidator<ConfigurationModel>>();
            var validationResult = validator.Validate(new ConfigurationModel
            {
                ApiToken = openPayPaymentSettings.ApiToken,
                RegionTwoLetterIsoCode = openPayPaymentSettings.RegionTwoLetterIsoCode,
                PlanTiers = openPayPaymentSettings.PlanTiers
            });

            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));
                return (false, errors);
            }

            // check the primary store currency is available
            var region = Defaults.OpenPay.AvailableRegions.FirstOrDefault(
                region => region.IsSandbox == openPayPaymentSettings.UseSandbox && region.TwoLetterIsoCode == openPayPaymentSettings.RegionTwoLetterIsoCode);

            var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (primaryStoreCurrency.CurrencyCode != region.CurrencyCode)
            {
                var invalidCurrencyLocale = _localizationService.GetResource("Plugins.Payments.OpenPay.InvalidCurrency");
                var invalidCurrencyMessage = string.Format(invalidCurrencyLocale, region.TwoLetterIsoCode, region.CurrencyCode);
                errors.Add(invalidCurrencyLocale);

                return (false, errors);
            }

            return (true, errors);
        }

        /// <summary>
        /// Returns the value indicating whether to payment method can be displayed in checkout
        /// </summary>
        /// <param name="cart">The shopping cart</param>
        /// <returns>The value indicating whether to payment method can be displayed in checkout</returns>
        public virtual bool CanDisplayPaymentMethod(IList<ShoppingCartItem> cart)
        {
            if (cart is null)
                throw new ArgumentNullException(nameof(cart));

            if (!Validate().IsValid)
                return false;   

            if (_openPayPaymentSettings.MinOrderTotal == 0 || _openPayPaymentSettings.MaxOrderTotal == 0)
                return false;

            var cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);
            if (!cartTotal.HasValue)
            {
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart, true, out _, out _, out _, out var subTotalWithDiscount);
                cartTotal = subTotalWithDiscount;
            }

            if (cartTotal < _openPayPaymentSettings.MinOrderTotal || cartTotal > _openPayPaymentSettings.MaxOrderTotal)
                return false;

            if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
                return false;

            return true;
        }

        /// <summary>
        /// Returns the value indicating whether to widget can be displayed in public
        /// </summary>
        /// <returns>The value indicating whether to widget can be displayed in public</returns>
        public virtual bool CanDisplayWidget()
        {
            if (!Validate().IsValid)
                return false;

            if (_openPayPaymentSettings.MinOrderTotal == 0 || _openPayPaymentSettings.MaxOrderTotal == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Places order and returns the handover URL to redirect user when order is placed successful; otherwise returns null with the errors as <see cref="IList{string}"/>
        /// </summary>
        /// <param name="order">The order</param>
        /// <returns>The <see cref="Task"/> containing the handover URL to redirect user when order is placed successful; otherwise returns null with the errors as <see cref="IList{string}"/></returns>
        public virtual async Task<(string HandoverUrl, IList<string> Errors)> PlaceOrderAsync(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var validationResult = Validate();
            if (!validationResult.IsValid)
                return (null, validationResult.Errors);

            var errors = new List<string>();

            var shippingAddressId = order.PickupInStore ? order.PickupAddressId : order.ShippingAddressId;
            if (!shippingAddressId.HasValue)
            {
                errors.Add($"Cannot process payment for order {order.CustomOrderNumber}. The shipping address not found.");
                return (null, errors);
            }

            var shippingAddress = _addressService.GetAddressById(shippingAddressId.Value);
            if (shippingAddress == null)
            {
                errors.Add($"Cannot process payment for order {order.CustomOrderNumber}. The shipping address not found.");
                return (null, errors);
            }

            if (!shippingAddress.StateProvinceId.HasValue)
            {
                errors.Add($"Cannot process payment for order {order.CustomOrderNumber}. The state not found.");
                return (null, errors);
            }

            var shippingState = _stateProvinceService.GetStateProvinceById(shippingAddress.StateProvinceId.Value);
            if (shippingState == null)
            {
                errors.Add($"Cannot process payment for order {order.CustomOrderNumber}. The state not found.");
                return (null, errors);
            }

            var deliveryAddress = new CustomerAddress
            {
                Line1 = shippingAddress.Address1,
                Line2 = shippingAddress.Address2,
                Suburb = shippingAddress.City ?? shippingAddress.County,
                PostCode = shippingAddress.ZipPostalCode,
                State = shippingState.Abbreviation
            };

            var customer = _customerService.GetCustomerById(order.CustomerId);
            if (customer == null)
            {
                errors.Add($"Cannot process payment for order {order.CustomOrderNumber}. The customer not found.");
                return (null, errors);
            }

            var customerDetails = new PersonalDetails
            {
                Email = shippingAddress.Email ?? customer.Email,
                DeliveryAddress = deliveryAddress
            };

            if (!order.PickupInStore && !string.IsNullOrWhiteSpace(shippingAddress.FirstName))
                customerDetails.FirstName = shippingAddress.FirstName;
            else
            {
                customerDetails.FirstName = _customerSettings.FirstNameEnabled
                    ? _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.FirstNameAttribute)
                    : _customerSettings.UsernamesEnabled ? customer.Username : customer.Email;
            }

            if (!order.PickupInStore && !string.IsNullOrWhiteSpace(shippingAddress.LastName))
                customerDetails.FamilyName = shippingAddress.LastName;
            else
            {
                customerDetails.FamilyName = _customerSettings.LastNameEnabled
                    ? _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.LastNameAttribute)
                    : _customerSettings.UsernamesEnabled ? customer.Username : customer.Email;
            }

            var currentRequestProtocol = _webHelper.CurrentRequestProtocol;
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
            var callbackUrl = urlHelper.RouteUrl(Defaults.SuccessfulPaymentWebhookRouteName, null, currentRequestProtocol);
            var failUrl = urlHelper.RouteUrl(Defaults.OrderDetailsRouteName, new { orderId = order.Id }, currentRequestProtocol);

            var customerJourney = new CustomerJourney
            {
                Origin = "Online",
                Online = new OnlineJourneyDetails
                {
                    CallbackUrl = callbackUrl,
                    CancelUrl = failUrl,
                    FailUrl = failUrl,
                    CustomerDetails = customerDetails,
                    PlanCreationType = "pending",
                    DeliveryMethod = order.PickupInStore ? "Pickup" : "Delivery"
                }
            };

            var createOrderRequest = new CreateOrderRequest
            {
                PurchasePrice = (int)(order.OrderTotal * 100),
                CustomerJourney = customerJourney,
                RetailerOrderNo = order.Id.ToString()
            };

            var orderItems = _orderService.GetOrderItems(order.Id);
            if (orderItems?.Any() == true)
            {
                var cartItems = new List<CartItem>();
                foreach (var item in orderItems)
                {
                    var product = _productService.GetProductById(item.ProductId);
                    if (product == null)
                    {
                        errors.Add($"Cannot process payment for order {order.CustomOrderNumber}. Cannot get the product by id '{item.ProductId}'.");
                        return (null, errors);
                    }

                    var productName = string.Empty;
                    if (string.IsNullOrEmpty(item.AttributesXml))
                        productName = product.Name;
                    else
                    {
                        var attributeInfo = _productAttributeFormatter.FormatAttributes(product, item.AttributesXml, customer, ", ");
                        productName = $"{product.Name} ({attributeInfo})";
                    }

                    var cartItem = new CartItem
                    {
                        Name = productName,
                        Code = product.Id.ToString(),
                        Quantity = item.Quantity.ToString(),
                        UnitPrice = (int)(item.UnitPriceInclTax * 100),
                        Charge = (int)(item.PriceInclTax * 100)
                    };

                    cartItems.Add(cartItem);
                }

                createOrderRequest.CartItems = cartItems.ToArray();
            }

            try
            {
                var openPayOrder = await _openPayApi.CreateOrderAsync(createOrderRequest);
                var formPost = openPayOrder?.NextAction?.FormPost;
                if (!string.IsNullOrEmpty(formPost?.FormPostUrl) && formPost?.FormFields?.Any() == true)
                {
                    // add query directly to eliminate character escaping
                    var redirectUrl = $"{formPost?.FormPostUrl}?{string.Join("&", formPost.FormFields.Select(field => $"{field.Name}={field.Value}"))}";
                    
                    return (redirectUrl, errors);
                }

                errors.Add($"Cannot process payment for order {order.CustomOrderNumber}. Cannot get the handover URL to redirect user to OpenPay gateway.");
            }
            catch (ApiException ex)
            {
                errors.Add(ex.Message);
            }

            return (null, errors);
        }

        /// <summary>
        /// Captures order and returns the captured order id; otherwise returns null with the errors as <see cref="IList{string}"/>
        /// </summary>
        /// <param name="order">The order</param>
        /// <returns>The <see cref="Task"/> containing the captured order id; otherwise returns null with the errors as <see cref="IList{string}"/></returns>
        public virtual async Task<(string OrderId, IList<string> Errors)> CaptureOrderAsync(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var validationResult = Validate();
            if (!validationResult.IsValid)
                return (null, validationResult.Errors);

            var errors = new List<string>();

            try
            {
                var orderStatus = await _openPayApi.GetOrderStatusByRetailerIdAsync(order.Id.ToString());
                if (orderStatus == null)
                {
                    errors.Add($"Cannot capture payment for order {order.CustomOrderNumber}. Cannot get the OpenPay order by retailer order id '{order.Id}'.");
                    return (null, errors);
                }

                var captureResponse = await _openPayApi.CaptureOrderByIdAsync(orderStatus.OrderId);
                if (captureResponse == null)
                {
                    errors.Add($"Cannot capture payment for order {order.CustomOrderNumber}.");
                    return (null, errors);
                }

                return (captureResponse.OrderId, errors);
            }
            catch (ApiException ex)
            {
                errors.Add(ex.Message);
            }

            return (null, errors);
        }

        /// <summary>
        /// Returns the value indicating whether to refund was created successfully; otherwise returns the errors as <see cref="IList{string}"/>
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="order">The amount to refund.</param>
        /// <returns>The <see cref="Task"/> containing the value indicating whether to refund was created successfully; otherwise returns the errors as <see cref="IList{string}"/></returns>
        public virtual async Task<(bool IsSuccess, IList<string> Errors)> RefundOrderAsync(Order order, decimal? amountToRefund = null)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var validationResult = Validate();
            if (!validationResult.IsValid)
                return (false, validationResult.Errors);

            var errors = new List<string>();

            if (string.IsNullOrEmpty(order.CaptureTransactionId))
            {
                errors.Add($"Cannot refund the OpenPay order. The captured transaction id is null.");
                return (false, errors);
            }

            try
            {
                var createRefundRequest = new CreateRefundRequest
                {
                    FullRefund = !amountToRefund.HasValue,
                    ReducePriceBy = amountToRefund.HasValue 
                        ? (int)(amountToRefund.Value * 100)
                        : 0
                };
                var createRefundResponse = await _openPayApi.CreateRefundAsync(order.CaptureTransactionId, createRefundRequest);
                if (createRefundResponse == null)
                {
                    errors.Add($"Cannot refund the OpenPay order. Cannot create the OpenPay refund by captured order id '{order.CaptureTransactionId}'.");
                    return (false, errors);
                }

                return (true, errors);
            }
            catch (ApiException ex)
            {
                errors.Add(ex.Message);
            }

            return (false, errors);
        }

        #endregion
    }
}
