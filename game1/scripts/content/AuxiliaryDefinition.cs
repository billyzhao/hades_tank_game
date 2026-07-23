using Godot;

namespace Game1;

/// <summary>自动辅助系统的只读内容配置；运行时状态永不写回该资源。</summary>
[GlobalClass]
public partial class AuxiliaryDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public string Description { get; set; } = string.Empty;
    [Export] public AuxiliaryTargetMode TargetMode { get; set; }
    [Export] public float BaseCooldown { get; set; } = 1f;
    [Export] public int MaximumRank { get; set; } = 3;
    [Export] public int BaseDamage { get; set; } = 6;
    [Export] public float Range { get; set; } = 105f;
}
