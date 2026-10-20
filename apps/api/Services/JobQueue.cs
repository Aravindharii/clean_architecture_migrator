using System.Threading.Channels;

namespace CleanArchitectureMigration.Api.Services;

public enum JobAction
{
    RunAnalysis,
    RunPlanning,
    RunTransformationAndValidation
}

public sealed record JobMessage(Guid JobId, JobAction Action, string? ExtraData = null);

public interface IJobQueue
{
    ValueTask EnqueueAsync(JobMessage message, CancellationToken cancellationToken = default);
    ValueTask<JobMessage> DequeueAsync(CancellationToken cancellationToken = default);
}

public sealed class ChannelJobQueue : IJobQueue
{
    private readonly Channel<JobMessage> _channel = Channel.CreateUnbounded<JobMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(JobMessage message, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public ValueTask<JobMessage> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
