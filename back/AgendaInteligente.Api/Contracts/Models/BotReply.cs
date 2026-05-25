namespace AgendaInteligente.Api.Contracts.Models;

/// <summary>
/// Resposta do bot: texto simples OU lista interativa (WhatsApp list message).
/// Use os factory methods — nunca o construtor diretamente.
/// </summary>
public sealed record BotReply
{
    public string?                  Text            { get; init; }
    public InteractiveListPayload?  InteractiveList { get; init; }
    public bool HasInteractive => InteractiveList is not null;

    private BotReply() { }

    public static BotReply Empty                                  => new();
    public static BotReply FromText(string text)                  => new() { Text = text };
    public static BotReply FromInteractiveList(InteractiveListPayload p) => new() { InteractiveList = p };
}
