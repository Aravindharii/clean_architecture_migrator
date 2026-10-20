using FluentValidation;
using LibraryManagement.Api.Common.Webhooks;
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Validators;

public class WebhookSubscriptionCreateRequestValidator : AbstractValidator<WebhookSubscriptionCreateRequest>
{
    public WebhookSubscriptionCreateRequestValidator()
    {
        RuleFor(x => x.CallbackUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Callback URL is required.")
            .MaximumLength(2000).WithMessage("Callback URL must not exceed 2000 characters.")
            .Must(BeValidHttpUrl).WithMessage("Callback URL must be a valid http or https URL.");

        RuleFor(x => x.EventTypes)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("At least one event type is required.")
            .NotEmpty().WithMessage("Select at least one event type.")
            .Must(HaveNoDuplicates).WithMessage("Event types must not contain duplicates.")
            .Must(AllBeKnown).WithMessage("One or more event types are not supported.");

        RuleForEach(x => x.EventTypes)
            .NotEmpty().WithMessage("Event types must not contain empty values.");
    }

    private static bool BeValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool HaveNoDuplicates(List<string> eventTypes) =>
        eventTypes.Distinct().Count() == eventTypes.Count;

    private static bool AllBeKnown(List<string> eventTypes) =>
        eventTypes.All(WebhookEventTypes.IsValid);
}
