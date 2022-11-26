using System.Collections.Generic;
using UnityEngine;

public class AnimatingSpriteProxy : InscryptionUIProxy
{
    protected override string InternalTypeName => "AnimatingSprite";

    public List<Sprite> frames = new();
    public List<Texture2D> textureFrames = new();
    public string textureField = "_MainTex";
    public bool randomizeSprite;
    public bool noRepeat;
    public bool setFirstFrameOnAwake = true;
    public float animSpeed = 0.033f;
    public float animOffset;
    public int frameOffset;
    public bool randomOffset;
    public bool stopAfterSingleIteration;
    public bool pingpong;
    public bool updateWhenPaused;
    public string soundId;
    public int soundFrame;
    public List<Sprite> blinkFrames = new();
    public float blinkRate;
    public float doubleBlinkRate;
}