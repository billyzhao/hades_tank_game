using System.Collections.Generic;

namespace Game1;

/// <summary>核心的只读基础定义；运行期状态由 RunState 与 BuildController 保存，禁止修改本定义。</summary>
public sealed record CoreDefinition(
    CoreId Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> BuildTags,
    IReadOnlyList<StatModifier> InitialModifiers);
