using System;

namespace XTempTraits;

public class Trait : Vintagestory.GameContent.Trait 
{
    public long Time;

    public bool IsTemp()
    {
        return Time > 0;
    }

    public override string ToString()
    {
        return $"Trait({Code})";
    }
}