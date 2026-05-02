namespace Digiprise.PMS.Contracts.DTOs;

public record BaselineIssueItem(
    IssueDto Current,
    string? BaselineChangedFieldsJson,
    string BaselineStatus
);

public record BaselineResponse(
    IEnumerable<BaselineIssueItem> Items,
    int Total
);
