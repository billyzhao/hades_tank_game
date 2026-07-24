using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>统一创建短寿命像素序列，确保最近邻采样、层级和销毁时机一致。</summary>
public static class SpriteEffectPlayer
{
    public static AnimatedSprite2D Create(
        string name,
        IReadOnlyList<Texture2D> textures,
        float framesPerSecond,
        bool loop = false)
    {
        SpriteFrames frames = new();
        frames.SetAnimationSpeed("default", framesPerSecond);
        frames.SetAnimationLoopMode(
            "default",
            loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
        foreach (Texture2D texture in textures) frames.AddFrame("default", texture);

        return new AnimatedSprite2D
        {
            Name = name,
            SpriteFrames = frames,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Centered = true
        };
    }

    public static AnimatedSprite2D Spawn(
        Node parent,
        Vector2 globalPosition,
        IReadOnlyList<Texture2D> textures,
        float framesPerSecond = 14f,
        float scale = 1f,
        int zIndex = 20,
        Color? modulate = null)
    {
        AnimatedSprite2D effect = Create("PixelEffect", textures, framesPerSecond);
        parent.AddChild(effect);
        effect.GlobalPosition = globalPosition;
        effect.Scale = Vector2.One * scale;
        effect.ZIndex = zIndex;
        effect.Modulate = modulate ?? Colors.White;
        effect.Play();

        double lifetime = textures.Count / Mathf.Max(1f, framesPerSecond);
        Tween cleanup = effect.CreateTween();
        cleanup.TweenInterval(lifetime + .04f);
        cleanup.TweenCallback(Callable.From(effect.QueueFree));
        return effect;
    }
}
