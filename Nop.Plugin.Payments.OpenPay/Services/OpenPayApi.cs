using System;
using System.Net.Http;
using System.Threading.Tasks;
using Nop.Plugin.Payments.OpenPay.Models;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    /// <summary>
    /// Provides an default implementation of the HTTP client to interact with the OpenPay endpoints
    /// </summary>
    public class OpenPayApi : BaseHttpClient
    {
        #region Ctor

        public OpenPayApi(OpenPayPaymentSettings settings, HttpClient httpClient)
            : base(settings, httpClient)
        {
        }

        #endregion

        #region Methods

        public virtual Task<OrderLimits> GetOrderLimitsAsync()
        {
            return GetAsync<OrderLimits>("/v1/merchant/orders/limits");
        }

        public virtual Task<CustomerOrderStatus> GetOrderStatusByIdAsync(string orderId)
        {
            return GetAsync<CustomerOrderStatus>($"/v1/merchant/orders/{orderId}");
        }

        public virtual Task<CustomerOrderStatus> GetOrderStatusByRetailerIdAsync(string retailerOrderId)
        {
            return GetAsync<CustomerOrderStatus>($"/v1/merchant/orders/retailer/{retailerOrderId}");
        }

        public virtual Task<CustomerOrder> CreateOrderAsync(CreateOrderRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<CustomerOrder>("/v1/merchant/orders", request);
        }

        public virtual Task<OpenPayEntity> CaptureOrderByIdAsync(string orderId)
        {
            if (orderId is null)
                throw new ArgumentNullException(nameof(orderId));

            return PostAsync<OpenPayEntity>($"/v1/merchant/orders/{orderId}/capture");
        }

        public virtual Task<OpenPayEntity> CreateRefundAsync(string orderId, CreateRefundRequest request)
        {
            if (orderId is null)
                throw new ArgumentNullException(nameof(orderId));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<OpenPayEntity>($"/v1/merchant/orders/{orderId}/refund", request);
        }

        #endregion
    }
}
