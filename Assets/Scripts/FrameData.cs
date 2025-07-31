using UnityEngine;

[System.Serializable]
public class FrameData
{
    public string frameType;
    public Sprite frameSprite;

    public FrameData(string type, Sprite sprite)
    {
        frameType = type;
        frameSprite = sprite;
    }
}