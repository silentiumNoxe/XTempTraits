namespace XTempTraits;

public class Trait : Vintagestory.GameContent.Trait 
{
    public long Time;

    public bool IsTemp()
    {
        return Time > 0;
    }
}