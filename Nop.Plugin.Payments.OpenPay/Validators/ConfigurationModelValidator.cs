using FluentValidation;
using Nop.Plugin.Payments.OpenPay.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.OpenPay.Validators
{
    /// <summary>
    /// Represents a validator for <see cref="ConfigurationModel"/>
    /// </summary>
    public class ConfigurationModelValidator : BaseNopValidator<ConfigurationModel>
    {
        #region Ctor

        public ConfigurationModelValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.ApiToken)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.OpenPay.Fields.ApiToken.Required"));

            RuleFor(model => model.RegionTwoLetterIsoCode)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.OpenPay.Fields.RegionTwoLetterIsoCode.Required"));

            RuleFor(model => model.PlanTiers)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.OpenPay.Fields.PlanTiers.Required"));
        }

        #endregion
    }
}
