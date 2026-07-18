using Godot;

namespace Game1;

/// <summary>房间资源中描述一波敌军的只读数据；06B 仅承载既有敌军行为类型，不扩充敌军池。</summary>
[GlobalClass]
public partial class RoomWaveDefinition : Resource
{
    [Export] public Godot.Collections.Array<BehaviorId> Behaviors { get; set; } = new();
}
