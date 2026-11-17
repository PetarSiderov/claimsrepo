namespace Claims.Auditing;

public sealed record AuditEntry(string EntityType, string EntityId, string HttpRequestType, DateTime CreatedAt);
