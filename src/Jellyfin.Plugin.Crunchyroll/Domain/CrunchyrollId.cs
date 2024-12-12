namespace Jellyfin.Plugin.Crunchyroll.Domain;

public record CrunchyrollId
{
    private string Value { get; init; }

    public CrunchyrollId(string value)
    {
        Value = value;
    }
    
    public static implicit operator CrunchyrollId(string value)
    {
        return new CrunchyrollId(value);
    }
    
    public static implicit operator string(CrunchyrollId crunchyrollId)
    {
        return crunchyrollId.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}