using UnityEngine;
using UnityEngine.UI;

public class PixelTextProxy : InscryptionUIProxy
{
    protected override string InternalTypeName => "PixelText";

    public Text mainText;
    public Font defaultFont;
    public Font smallFont;
}