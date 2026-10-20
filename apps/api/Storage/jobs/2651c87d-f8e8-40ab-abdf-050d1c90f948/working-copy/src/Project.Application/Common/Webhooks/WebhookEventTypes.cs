namespace Project.Application.Common.Webhooks;

public static class WebhookEventTypes
{
    public const string LoanCreated = "loan.created";
    public const string LoanRenewed = "loan.renewed";
    public const string LoanReturned = "loan.returned";
    public const string UserDeleted = "user.deleted";
    public const string BookCreated = "book.created";
    public const string BookUpdated = "book.updated";
    public const string BookDeleted = "book.deleted";
    public const string CopyStatusChanged = "copy.statusChanged";

    public static readonly IReadOnlyList<string> All =
    [
        LoanCreated,
        LoanRenewed,
        LoanReturned,
        UserDeleted,
        BookCreated,
        BookUpdated,
        BookDeleted,
        CopyStatusChanged
    ];

    public static bool IsValid(string eventType) => All.Contains(eventType);
}
