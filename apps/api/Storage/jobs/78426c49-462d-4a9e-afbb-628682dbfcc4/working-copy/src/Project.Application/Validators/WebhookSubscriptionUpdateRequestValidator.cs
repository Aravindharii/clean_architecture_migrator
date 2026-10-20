using FluentValidation;
using Project.Application.Common.Webhooks;
using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

public class WebhookSubscriptionUpdateRequestValidator : AbstractValidator<WebhookSubscriptionUpdateRequest>
{
    public WebhookSubscriptionUpdateRequestValidator()
    {
        When(x => x.CallbackUrl is not null, () =>
        {
            RuleFor(x => x.CallbackUrl)
                .NotEmpty().WithMessage("Callback URL is required.")
                .MaximumLength(2000).WithMessage("Callback URL must not exceed 2000 characters.")
                .Must(BeValidHttpUrl).WithMessage("Callback URL must be a valid http or https URL.");
        });

        When(x => x.EventTypes is not null, () =>
        {
            RuleFor(x => x.EventTypes)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Select at least one event type.")
                .Must(HaveNoDuplicates).WithMessage("Event types must not contain duplicates.")
                .Must(AllBeKnown).WithMessage("One or more event types are not supported.");

            RuleForEach(x => x.EventTypes)
                .NotEmpty().WithMessage("Event types must not contain empty values.");
        });
    }

    private static bool BeValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool HaveNoDuplicates(List<string>? eventTypes) =>
        eventTypes is not null && eventTypes.Distinct().Count() == eventTypes.Count;

    private static bool AllBeKnown(List<string>? eventTypes) =>
        eventTypes is not null && eventTypes.All(WebhookEventTypes.IsValid);
}
