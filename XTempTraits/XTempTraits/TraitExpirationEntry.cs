namespace XTempTraits;

public class TraitExpirationEntry
{
    public string Code;
    public double ExpiredAt;

    public TraitExpirationEntry(string code, double expiredAt)
    {
        Code = code;
        ExpiredAt = expiredAt;
    }
}