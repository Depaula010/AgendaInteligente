using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AgendaInteligente.Api.Services.BackgroundServices;

/// <summary>
/// Consome o stream Redis `whatsapp:inbound`, processa via IWebhookService
/// e envia a resposta de volta ao bot via IWhatsAppSendService.
/// Registrado apenas quando Redis está configurado (ver Program.cs).
/// </summary>
public sealed class InboundStreamConsumerService : BackgroundService
{
    private readonly IConnectionMultiplexer                    _redis;
    private readonly IServiceScopeFactory                      _scopeFactory;
    private readonly RedisStreamOptions                        _opts;
    private readonly ILogger<InboundStreamConsumerService>     _logger;

    public InboundStreamConsumerService(
        IConnectionMultiplexer                 redis,
        IServiceScopeFactory                   scopeFactory,
        IOptions<RedisStreamOptions>            opts,
        ILogger<InboundStreamConsumerService>  logger)
    {
        _redis        = redis;
        _scopeFactory = scopeFactory;
        _opts         = opts.Value;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureConsumerGroupAsync();

        _logger.LogInformation(
            "[STREAM-CONSUMER] Iniciado. Stream={Stream} Group={Group} Consumer={Consumer}",
            _opts.InboundStream, _opts.ConsumerGroup, _opts.ConsumerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var db      = _redis.GetDatabase();
                var entries = await db.StreamReadGroupAsync(
                    _opts.InboundStream,
                    _opts.ConsumerGroup,
                    _opts.ConsumerName,
                    ">",
                    count: _opts.BatchSize,
                    noAck: false);

                if (entries is { Length: > 0 })
                {
                    foreach (var entry in entries)
                    {
                        await ProcessEntryAsync(entry, stoppingToken);
                    }
                }
                else
                {
                    // Nenhuma mensagem nova — aguarda antes de tentar novamente
                    await Task.Delay(_opts.BlockMs, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "[STREAM-CONSUMER] Erro Redis — aguardando 5s antes de tentar novamente.");
                await Task.Delay(5_000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STREAM-CONSUMER] Erro inesperado no loop de consumo.");
                await Task.Delay(1_000, stoppingToken);
            }
        }

        _logger.LogInformation("[STREAM-CONSUMER] Encerrado graciosamente.");
    }

    private async Task EnsureConsumerGroupAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StreamCreateConsumerGroupAsync(
                _opts.InboundStream,
                _opts.ConsumerGroup,
                StreamPosition.NewMessages,
                createStream: true);

            _logger.LogInformation(
                "[STREAM-CONSUMER] Consumer group '{Group}' criado/confirmado em '{Stream}'.",
                _opts.ConsumerGroup, _opts.InboundStream);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Grupo já existe — normal em redeploys
            _logger.LogDebug("[STREAM-CONSUMER] Consumer group já existe (BUSYGROUP). Continuando.");
        }
    }

    private async Task ProcessEntryAsync(StreamEntry entry, CancellationToken ct)
    {
        var db = _redis.GetDatabase();

        string? tenantIdStr      = entry["tenant_id"];
        string? numeroRemetente  = entry["numero_remetente"];
        string? texto            = entry["texto"];

        if (!Guid.TryParse(tenantIdStr, out var tenantId) ||
            string.IsNullOrWhiteSpace(numeroRemetente)    ||
            string.IsNullOrWhiteSpace(texto))
        {
            _logger.LogWarning(
                "[STREAM-CONSUMER] Entrada inválida ignorada. EntryId={Id} TenantId={TenantId}",
                entry.Id, tenantIdStr);
            await db.StreamAcknowledgeAsync(_opts.InboundStream, _opts.ConsumerGroup, entry.Id);
            return;
        }

        _logger.LogDebug(
            "[STREAM-CONSUMER] Processando mensagem. EntryId={Id} TenantId={TenantId} Phone={Phone}",
            entry.Id, tenantId, numeroRemetente);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var webhookService  = scope.ServiceProvider.GetRequiredService<IWebhookService>();
            var sendService     = scope.ServiceProvider.GetRequiredService<IWhatsAppSendService>();

            var request = new BotWebhookRequest
            {
                NumeroRemetente = numeroRemetente,
                Texto           = texto,
            };

            var reply = await webhookService.ProcessWhatsAppMessageAsync(tenantId, request, ct);

            if (!string.IsNullOrWhiteSpace(reply))
            {
                await sendService.SendTextMessageAsync(tenantId, numeroRemetente, reply, ct);
            }

            await db.StreamAcknowledgeAsync(_opts.InboundStream, _opts.ConsumerGroup, entry.Id);

            _logger.LogInformation(
                "[STREAM-CONSUMER] Mensagem processada e ACK enviado. EntryId={Id} TenantId={TenantId}",
                entry.Id, tenantId);
        }
        catch (Exception ex)
        {
            // Sem ACK — mensagem volta para a PEL e pode ser reprocessada
            _logger.LogError(ex,
                "[STREAM-CONSUMER] Falha ao processar entrada. EntryId={Id} TenantId={TenantId} — sem ACK para reprocessamento.",
                entry.Id, tenantId);
        }
    }
}
