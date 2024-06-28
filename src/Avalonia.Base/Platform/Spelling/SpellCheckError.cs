namespace Avalonia.Platform.Spelling;

public class SpellCheckError
{
    public int StartIndex { get; set; }
    public int Length { get; init; }
    public SpellCheckCorrectiveAction Action { get; init; }
    public string? Replace { get; init; }

}
