using Godot;

namespace Game1;

/// <summary>同一种辅助机 Mk.I 至 Mk.III 的只读视觉谱系。</summary>
[GlobalClass]
public partial class AuxiliaryVisualSet : Resource
{
    [Export] public string AuxiliaryId { get; set; } = string.Empty;
    [Export] public Texture2D RankOneTexture { get; set; } = null!;
    [Export] public Texture2D RankTwoTexture { get; set; } = null!;
    [Export] public Texture2D RankThreeTexture { get; set; } = null!;
    [Export] public AuxiliaryVisualMode Mode { get; set; }
    [Export] public Vector2 LocalOffset { get; set; }
    [Export] public float RankOneScale { get; set; } = 1f;
    [Export] public float RankTwoScale { get; set; } = 0.24f;
    [Export] public float RankThreeScale { get; set; } = 0.27f;
    [Export] public float OrbitRadius { get; set; } = 15f;

    public Texture2D TextureForRank(int rank) => rank switch
    {
        1 => RankOneTexture,
        2 => RankTwoTexture,
        3 => RankThreeTexture,
        _ => throw new System.ArgumentOutOfRangeException(nameof(rank))
    };

    public float ScaleForRank(int rank) => rank switch
    {
        1 => RankOneScale,
        2 => RankTwoScale,
        3 => RankThreeScale,
        _ => throw new System.ArgumentOutOfRangeException(nameof(rank))
    };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AuxiliaryId) || AuxiliaryId != AuxiliaryId.Trim())
            throw new System.ArgumentException("辅助视觉必须提供稳定 Id。", nameof(AuxiliaryId));
        if (RankOneTexture is null || RankTwoTexture is null || RankThreeTexture is null)
            throw new System.ArgumentException($"辅助 '{AuxiliaryId}' 必须配置三阶贴图。");
        if (!System.Enum.IsDefined(Mode)) throw new System.ArgumentOutOfRangeException(nameof(Mode));
        foreach (float scale in new[] { RankOneScale, RankTwoScale, RankThreeScale })
        {
            if (!float.IsFinite(scale) || scale is <= 0f or > 1.5f)
                throw new System.ArgumentOutOfRangeException(nameof(RankOneScale));
        }
        if (!float.IsFinite(OrbitRadius) || OrbitRadius is < 0f or > 64f)
            throw new System.ArgumentOutOfRangeException(nameof(OrbitRadius));
    }
}
