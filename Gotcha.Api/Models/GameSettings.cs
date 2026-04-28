namespace Gotcha.Api.Models;

public class GameSettings
{
    public int DefaultGuessesAllowed { get; set; } = 6;
    public int DefaultBonusGuessesOnSwap { get; set; } = 3;
    public int WordLength { get; set; } = 5;
}
