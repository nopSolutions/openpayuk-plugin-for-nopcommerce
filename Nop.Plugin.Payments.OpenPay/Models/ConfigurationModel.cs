using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel;

namespace Nop.Plugin.Payments.OpenPay.Models
{
    /// <summary>
    /// Represents a configuration model
    /// </summary>
    public class ConfigurationModel : BaseNopModel
    {
        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sandbox environment is active
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the token to sign the API requests
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.ApiToken")]
        public string ApiToken { get; set; }
        public bool ApiToken_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a region two letter ISO code
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.RegionTwoLetterIsoCode")]
        public string RegionTwoLetterIsoCode { get; set; }
        public bool RegionTwoLetterIsoCode_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the available regions
        /// </summary>
        public IList<SelectListItem> AvailableRegions { get; set; }

        /// <summary>
        /// Gets or sets the min order total
        /// </summary>
        [ReadOnly(true)]
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.MinOrderTotal")]
        public int MinOrderTotal { get; set; }
        public bool MinOrderTotal_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the max order total
        /// </summary>
        [ReadOnly(true)]
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.MaxOrderTotal")]
        public int MaxOrderTotal { get; set; }
        public bool MaxOrderTotal_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the price breakdown should be displayed on product page.
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.DisplayProductPageWidget")]
        public bool DisplayProductPageWidget { get; set; }
        public bool DisplayProductPageWidget_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the product page widget logo
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.ProductPageWidgetLogo")]
        public string ProductPageWidgetLogo { get; set; }
        public bool ProductPageWidgetLogo_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the product page widget logo position
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.ProductPageWidgetLogoPosition")]
        public string ProductPageWidgetLogoPosition { get; set; }
        public bool ProductPageWidgetLogoPosition_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the price breakdown should be displayed on product box.
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.DisplayProductListingWidget")]
        public bool DisplayProductListingWidget { get; set; }
        public bool DisplayProductListingWidget_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the product listing widget logo
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.ProductListingWidgetLogo")]
        public string ProductListingWidgetLogo { get; set; }
        public bool ProductListingWidgetLogo_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the logo should be hidden on the product listing widget.
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.ProductListingHideLogo")]
        public bool ProductListingHideLogo { get; set; }
        public bool ProductListingHideLogo_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the price breakdown should be displayed on shopping cart.
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.DisplayCartWidget")]
        public bool DisplayCartWidget { get; set; }
        public bool DisplayCartWidget_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the cart widget logo
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.CartWidgetLogo")]
        public string CartWidgetLogo { get; set; }
        public bool CartWidgetLogo_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the info belt should be displayed in the page head.
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.DisplayInfoBeltWidget")]
        public bool DisplayInfoBeltWidget { get; set; }
        public bool DisplayInfoBeltWidget_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the info belt widget color
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.InfoBeltWidgetColor")]
        public string InfoBeltWidgetColor { get; set; }
        public bool InfoBeltWidgetColor_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to the landing page link should be displayed in footer links.
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.DisplayLandingPageWidget")]
        public bool DisplayLandingPageWidget { get; set; }
        public bool DisplayLandingPageWidget_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets the available plan tiers you have available in months. E.g. [2,4,6] for 2 months, 4 months and 6 months
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.OpenPay.Fields.PlanTiers")]
        public string PlanTiers { get; set; }
        public bool PlanTiers_OverrideForStore { get; set; }

        #endregion

        #region Ctor

        public ConfigurationModel()
        {
            AvailableRegions = new List<SelectListItem>();
        }

        #endregion
    }
}