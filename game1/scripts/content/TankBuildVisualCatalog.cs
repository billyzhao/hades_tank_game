using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>
/// 坦克构筑外观的唯一静态目录。资源只读；协议和辅助的运行等级仍由 RunState/BuildController 持有。
/// </summary>
[GlobalClass]
public partial class TankBuildVisualCatalog : Resource
{
    [Export] public string Version { get; set; } = string.Empty;
    [Export] public Godot.Collections.Array<ProtocolDepartmentVisualDefinition> ProtocolVisuals { get; set; } = new();
    [Export] public Godot.Collections.Array<AuxiliaryVisualSet> AuxiliaryVisuals { get; set; } = new();

    public ProtocolDepartmentVisualDefinition GetProtocolVisual(ProtocolDepartment department) =>
        ProtocolVisuals.FirstOrDefault(item => item.Department == department)
        ?? throw new KeyNotFoundException($"未登记部门视觉：{department}");

    public AuxiliaryVisualSet GetAuxiliaryVisual(string auxiliaryId) =>
        AuxiliaryVisuals.FirstOrDefault(item => string.Equals(item.AuxiliaryId, auxiliaryId, StringComparison.Ordinal))
        ?? throw new KeyNotFoundException($"未登记辅助视觉：{auxiliaryId}");

    public void Validate(ContentCatalog content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(Version)) throw new ArgumentException("构筑视觉目录必须提供版本。", nameof(Version));
        if (ProtocolVisuals.Count != Enum.GetValues<ProtocolDepartment>().Length)
            throw new ArgumentException("构筑视觉目录必须恰好登记四个协议部门。");
        if (ProtocolVisuals.Any(item => item is null) ||
            ProtocolVisuals.Select(item => item.Department).Distinct().Count() != ProtocolVisuals.Count)
            throw new ArgumentException("协议部门视觉不得为空或重复。");
        foreach (ProtocolDepartmentVisualDefinition item in ProtocolVisuals) item.Validate();

        if (AuxiliaryVisuals.Any(item => item is null) ||
            AuxiliaryVisuals.Select(item => item.AuxiliaryId).Distinct(StringComparer.Ordinal).Count() != AuxiliaryVisuals.Count)
            throw new ArgumentException("辅助视觉不得为空或重复。");
        foreach (AuxiliaryVisualSet item in AuxiliaryVisuals) item.Validate();

        string[] expected = content.Auxiliaries.Select(item => item.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        string[] actual = AuxiliaryVisuals.Select(item => item.AuxiliaryId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            throw new ArgumentException("辅助视觉目录必须覆盖当前内容目录中的全部辅助系统。");
    }
}
