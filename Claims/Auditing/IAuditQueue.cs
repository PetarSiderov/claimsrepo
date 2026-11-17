namespace Claims.Auditing;

public interface IAuditQueue
{
    void Enqueue(AuditEntry entry);
    Task DrainAsync(CancellationToken cancellationToken = default);
}
