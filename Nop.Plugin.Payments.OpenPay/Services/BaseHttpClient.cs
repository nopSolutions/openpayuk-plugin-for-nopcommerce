using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Plugin.Payments.OpenPay.Models;

namespace Nop.Plugin.Payments.OpenPay.Services
{
    /// <summary>
    /// Provides an abstraction for the HTTP client to interact with the endponit.
    /// </summary>
    public abstract class BaseHttpClient
    {
        #region Fields

        private Lazy<HttpClient> _httpClient;
            
        #endregion

        #region Properties
        
        protected HttpClient HttpClient => _httpClient.Value;

        #endregion

        #region Ctor

        public BaseHttpClient(OpenPayPaymentSettings settings, HttpClient httpClient)
        {
            _httpClient = new Lazy<HttpClient>(() => 
            {
                var region = Defaults.OpenPay.AvailableRegions.FirstOrDefault(
                    region => region.IsSandbox == settings.UseSandbox && region.TwoLetterIsoCode == settings.RegionTwoLetterIsoCode);

                httpClient.BaseAddress = new Uri(region.ApiUrl, UriKind.RelativeOrAbsolute);
                httpClient.Timeout = TimeSpan.FromSeconds(Defaults.OpenPay.Api.DefaultTimeout);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, Defaults.OpenPay.Api.UserAgent);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "*/*");
                httpClient.DefaultRequestHeaders.Add(Defaults.OpenPay.Api.VersionHeaderName, Defaults.OpenPay.Api.Version);

                var userPasswordPair = settings.ApiToken.Replace("|", ":", StringComparison.InvariantCultureIgnoreCase);
                var userPasswordBytes = Encoding.ASCII.GetBytes(userPasswordPair);
                var encodedUserPasswordBytes = Convert.ToBase64String(userPasswordBytes);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedUserPasswordBytes);

                return httpClient;
            });
        }

        #endregion

        #region Methods

        protected virtual Task<TResponse> GetAsync<TResponse>(string requestUri, [CallerMemberName] string callerName = "")
        {
            return CallAsync<TResponse>(() => HttpClient.GetAsync(requestUri), callerName);
        }

        protected async virtual Task<TResponse> PostAsync<TResponse>(string requestUri, object request = null, [CallerMemberName] string callerName = "")
        {
            HttpContent body = null;
            if (request != null)
            {
                var content = JsonConvert.SerializeObject(request);
                body = new StringContent(content, Encoding.UTF8, MimeTypes.ApplicationJson);
            }

            return await CallAsync<TResponse>(() => HttpClient.PostAsync(requestUri, body), callerName);
        }

        protected virtual async Task<TResponse> CallAsync<TResponse>(Func<Task<HttpResponseMessage>> requestFunc, [CallerMemberName] string callerName = "")
        {
            HttpResponseMessage response = null;
            try
            {
                response = await requestFunc();
            }
            catch (Exception exception)
            {
                throw new ApiException(500, $"Error when calling '{callerName}'. HTTP status code - 500. {exception.Message}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;
            if (statusCode >= 400)
            {
                // throw exception with deserialized error
                var errorResponse = JsonConvert.DeserializeObject<ApiError>(responseContent);
                var message = $"Error when calling '{callerName}'. HTTP status code - {statusCode}. ";
                if (errorResponse != null)
                {
                    message += @$"
                            Title - '{errorResponse.Title}'.
                            Status - '{errorResponse.Status}'.
                            Type - '{errorResponse.Type}'.
                            Message - '{errorResponse.Detail}'.
                            Instance - '{errorResponse.Instance}'.";
                }

                throw new ApiException(statusCode, message, errorResponse);
            }

            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        #endregion
    }
}
