using UnityEngine;

public class MenuCardProxy : InscryptionUIProxy
{
    protected override string InternalTypeName => "MenuCard";

    public float raiseAmount = 0.2f;
    public float rotationRadius = 0.05f;
    public int menuAction;
    public bool permanentlyLocked;
    public Sprite titleSprite;
    public string titleText;
    public string titleLocId;
    public SpriteRenderer pixelBorder;
    public bool isAscensionCard;
    public bool lockBeforeStoryEvent;
    public bool lockAfterStoryEvent;
    public int storyEvent;
    public int storyEventAfter;
    public Sprite lockedTitleSprite;
    public GameObject glitchedCard;
}