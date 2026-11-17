using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;

namespace Claims.Auditing;

public class BackgroundAuditQueue : IAuditQueue, IHostedService
{
    private readonly Channel<AuditEntry> _entries;
    private readonly IServiceScopeFactory _scopeFactory;
    private Task? _processingTask;

    public BackgroundAuditQueue(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _entries = Channel.CreateUnbounded<AuditEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void Enqueue(AuditEntry entry)
    {
        _entries.Writer.TryWrite(entry);
    }

    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        while (await _entries.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_entries.Reader.TryRead(out var entry))
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AuditContext>();

                if (entry.EntityType == nameof(Claims.Models.Claim))
                {
                    context.ClaimAudits.Add(new ClaimAudit
                    {
                        Created = entry.CreatedAt,
                        HttpRequestType = entry.HttpRequestType,
                        ClaimId = entry.EntityId
                    });
                }
                else
                {
                    context.CoverAudits.Add(new CoverAudit
                    {
                        Created = entry.CreatedAt,
                        HttpRequestType = entry.HttpRequestType,
                        CoverId = entry.EntityId
                    });
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _processingTask = Task.Run(() => DrainAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _entries.Writer.TryComplete();

        if (_processingTask is not null)
        {
            await _processingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
