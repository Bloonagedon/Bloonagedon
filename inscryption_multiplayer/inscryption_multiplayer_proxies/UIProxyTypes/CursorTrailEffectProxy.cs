public class CursorTrailEffectProxy : InscryptionUIProxy
{
    protected override string InternalTypeName => "CursorTrailEffect";

    public float originalFrequency;
    public float volatility;
    public bool unscaledTime;
    public float cloneBrightness = 0.9f;
}