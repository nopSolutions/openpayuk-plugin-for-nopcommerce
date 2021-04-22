using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.OpenPay.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(Defaults.ConfigurationRouteName, "Plugins/OpenPay/Configure",
                new { controller = "OpenPayConfiguration", action = "Configure", area = AreaNames.Admin });

            endpointRouteBuilder.MapControllerRoute(Defaults.SuccessfulPaymentWebhookRouteName, "Plugins/OpenPay/Successful",
                new { controller = "OpenPayPayment", action = "SuccessfulPayment" });

            endpointRouteBuilder.MapControllerRoute(Defaults.OpenPayLandingPageRouteName, "Plugins/OpenPay/Landing",
                new { controller = "OpenPayPayment", action = "LandingPage" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}