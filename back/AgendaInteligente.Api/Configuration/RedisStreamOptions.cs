namespace AgendaInteligente.Api.Configuration;

public sealed class RedisStreamOptions
{
    public const string SectionName = "RedisStreams";

    public string InboundStream        { get; init; } = "whatsapp:inbound";
    public string DeadLetterStream     { get; init; } = "whatsapp:dead";
    public string ConsumerGroup        { get; init; } = "backend";
    public string ConsumerName         { get; init; } = "backend-1";
    public int    BatchSize            { get; init; } = 10;
    public int    BlockMs              { get; init; } = 2000;
    public int    DeadLetterThreshold  { get; init; } = 3;
}
