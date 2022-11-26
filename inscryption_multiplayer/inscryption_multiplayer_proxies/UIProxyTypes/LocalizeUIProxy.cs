public class LocalizeUIProxy : InscryptionUIProxy
{
    protected override string InternalTypeName => "LocalizeUI";

    public bool forceToUpper = true;
    public string untranslatedPrefix = "";
    public string untranslatedSuffix = "";
}