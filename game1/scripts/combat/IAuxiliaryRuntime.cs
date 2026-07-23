using Godot;

namespace Game1;

/// <summary>自动辅助运行时的统一生命周期合同。</summary>
public interface IAuxiliaryRuntime
{
    string AuxiliaryId { get; }
    void Configure(AuxiliaryDefinition definition, BuildSnapshot build);
    void Activate(Node2D owner);
    void Deactivate();
}
