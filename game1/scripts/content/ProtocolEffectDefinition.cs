using Godot;

namespace Game1;

/// <summary>只读协议资源中的单个效果声明。</summary>
[GlobalClass]
public partial class ProtocolEffectDefinition : Resource
{
    [Export] public string EffectId { get; set; } = string.Empty;

    [Export] public StatId Stat { get; set; }

    [Export] public float FlatAdd { get; set; }

    [Export] public float AdditivePercent { get; set; }

    [Export] public float MultiplicativePercent { get; set; }
}
