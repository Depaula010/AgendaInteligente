namespace AgendaInteligente.Api.Contracts.Models;

public sealed record InteractiveListPayload(
    string                          Title,
    string                          Body,
    string                          ButtonText,
    IReadOnlyList<InteractiveSection> Sections);

public sealed record InteractiveSection(
    string                              Title,
    IReadOnlyList<InteractiveSectionRow> Rows);

public sealed record InteractiveSectionRow(
    string  RowId,
    string  Title,
    string? Description = null);
