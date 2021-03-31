using System;
using System.Collections.Generic;
using System.Linq;
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
using Nop.Services.Common;
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
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IStateProvinceService _stateProvinceService;
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
            IOrderTotalCalculationService orderTotalCalculationService,
            IStateProvinceService stateProvinceService,
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
            _orderTotalCalculationService = orderTotalCalculationService;
            _stateProvinceService = stateProvinceService;
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
        /// <returns>The errors as <see cref="IList{string}"/> if plugin isn't configured; otherwise empty</returns>
        public virtual (bool IsValid, IList<string> Errors) Validate()
        {
            var errors = new List<string>();

            // resolve validator here to exclude warnings after installation process
            var validator = EngineContext.Current.Resolve<IValidator<ConfigurationModel>>();
            var validationResult = validator.Validate(new ConfigurationModel
            {
                ApiToken = _openPayPaymentSettings.ApiToken,
                RegionTwoLetterIsoCode = _openPayPaymentSettings.RegionTwoLetterIsoCode,
                PlanTiers = _openPayPaymentSettings.PlanTiers
            });

            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

                return (false, errors);
            }

            // check the primary store currency is available
            var region = Defaults.OpenPay.AvailableRegions.FirstOrDefault(
                region => region.IsSandbox == _openPayPaymentSettings.UseSandbox && region.TwoLetterIsoCode == _openPayPaymentSettings.RegionTwoLetterIsoCode);

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

            var cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);
            if (!cartTotal.HasValue)
                return false;

            if (_openPayPaymentSettings.MinOrderTotal == 0 || _openPayPaymentSettings.MaxOrderTotal == 0)
                return false;

            if (cartTotal < _openPayPaymentSettings.MinOrderTotal || cartTotal > _openPayPaymentSettings.MaxOrderTotal)
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
        /// <returns>The handover URL to redirect user when order is placed successful; otherwise returns null with the errors as <see cref="IList{string}"/></returns>
        public virtual (string HandoverUrl, IList<string> Errors) PlaceOrder(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var errors = new List<string>();

            var validationResult = Validate();
            if (!validationResult.IsValid)
                return (null, validationResult.Errors);

            var shippingAddress = order.PickupInStore
                ? _addressService.GetAddressById(order.PickupAddressId.Value)
                : _addressService.GetAddressById(order.ShippingAddressId.Value);
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

            var shippingState = _stateProvinceService
                .GetStateProvinceById(shippingAddress.StateProvinceId.Value);
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
                Email = customer.Email,
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

            try
            {
                var openPayOrder = _openPayApi.CreateOrderAsync(createOrderRequest).Result;
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
        /// <returns>The captured order id; otherwise returns null with the errors as <see cref="IList{string}"/></returns>
        public virtual (string OrderId, IList<string> Errors) CaptureOrder(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var errors = new List<string>();

            var validationResult = Validate();
            if (!validationResult.IsValid)
                return (null, validationResult.Errors);

            try
            {
                var orderStatus = _openPayApi.GetOrderStatusByRetailerIdAsync(order.Id.ToString()).Result;
                if (orderStatus == null)
                {
                    errors.Add($"Cannot capture payment for order {order.CustomOrderNumber}. Cannot get the OpenPay order by retailer order id '{order.Id}'.");
                    return (null, errors);
                }

                var captureResponse = _openPayApi.CaptureOrderByIdAsync(orderStatus.OrderId).Result;
                if (orderStatus == null)
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
        /// <returns>The value indicating whether to refund was created successfully; otherwise returns the errors as <see cref="IList{string}"/></returns>
        public virtual (bool IsSuccess, IList<string> Errors) RefundOrder(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            var errors = new List<string>();

            var validationResult = Validate();
            if (!validationResult.IsValid)
                return (false, validationResult.Errors);

            if (string.IsNullOrEmpty(order.CaptureTransactionId))
            {
                errors.Add($"Cannot refund the OpenPay order. The captured transaction id is null.");
                return (false, validationResult.Errors);
            }

            try
            {
                var createRefundRequest = new CreateRefundRequest
                {
                    FullRefund = true
                };
                var createRefundResponse = _openPayApi.CreateRefundAsync(order.CaptureTransactionId, createRefundRequest).Result;
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
