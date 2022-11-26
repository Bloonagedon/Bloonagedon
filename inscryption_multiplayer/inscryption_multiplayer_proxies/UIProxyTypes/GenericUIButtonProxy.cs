using GBC;
using UnityEngine;

public class GenericUIButtonProxy : InscryptionUIProxy
{
    protected override string InternalTypeName => nameof(GenericUIButton);

    public int cursorType;
    public bool changeSprite;
    public Sprite defaultSprite;
    public Sprite hoveringSprite;
    public Sprite downSprite;
    public Sprite disabledSprite;
    public GameObject pixelBorder;
    public bool disableBorderWhenInputDown;
    public Transform buttonContentsParent;
    public float contentsShiftAmount = 0.01f;
    public int inputButton;
    public KeyCode inputKey;
    public bool inputWhilePaused;
    public bool hiddenInGamepadMode;
}