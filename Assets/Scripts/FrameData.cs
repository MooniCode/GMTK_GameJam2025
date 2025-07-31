using UnityEngine;

[System.Serializable]
public class FrameData
{
    public string frameType;
    public Sprite frameSprite;
    public Sprite animationSprite;

    public FrameData(string type, Sprite sprite, Sprite timelineSprite = null)
    {
        frameType = type;
        frameSprite = sprite;
        this.animationSprite = timelineSprite;
    }

    public Sprite GetUISprite()
    {
        return frameSprite;
    }

    public Sprite GetAnimationSprite()
    {
        return animationSprite;
    }
}