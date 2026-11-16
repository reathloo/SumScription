namespace SumScription.Core.Calculator;

/// <summary>One calculator history row.</summary>
/// <param name="Expression">The evaluated expression.</param>
/// <param name="Value">The result as display text.</param>
/// <param name="IsMasked">Whether the parody paywall masks the result.</param>
public sealed record HistoryEntry(string Expression, string Value, bool IsMasked);
