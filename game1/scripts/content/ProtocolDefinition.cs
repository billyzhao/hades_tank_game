using Godot;

namespace Game1;

/// <summary>协议的只读内容定义；运行时不得修改此资源。</summary>
[GlobalClass]
public partial class ProtocolDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;

    [Export] public string DisplayName { get; set; } = string.Empty;

    [Export] public string Description { get; set; } = string.Empty;

    [Export] public ProtocolDepartment Department { get; set; }

    /// <summary>用于内容分层的正整数稀有度；数值越高代表越稀有。</summary>
    [Export] public int Rarity { get; set; } = 1;

    /// <summary>奖励候选池中的正有限基础权重。</summary>
    [Export] public float BaseWeight { get; set; } = 1f;

    /// <summary>选择本协议后提供给后续协议判定的稳定标签。</summary>
    [Export] public Godot.Collections.Array<string> Tags { get; set; } = new();

    /// <summary>必须已由本局先前选择提供的全部标签。</summary>
    [Export] public Godot.Collections.Array<string> RequiredTags { get; set; } = new();

    /// <summary>不能与本局先前选择标签同时存在的标签。</summary>
    [Export] public Godot.Collections.Array<string> ConflictTags { get; set; } = new();

    [Export] public int StackLimit { get; set; }

    [Export] public Godot.Collections.Array<string> ConflictIds { get; set; } = new();

    [Export] public Godot.Collections.Array<string> PrerequisiteIds { get; set; } = new();

    [Export] public Godot.Collections.Array<ProtocolEffectDefinition> Effects { get; set; } = new();
}
