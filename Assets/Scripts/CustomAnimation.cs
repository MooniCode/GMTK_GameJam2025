using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CustomAnimation
{
    public string animationType;
    public List<FrameData> frames;
    public float frameRate;
    public bool isLooping;

    public CustomAnimation(string type, List<FrameData> animFrames, float rate, bool loop = true)
    {
        animationType = type;
        frames = new List<FrameData>(animFrames);
        frameRate = rate;
        isLooping = loop;
    }
}
