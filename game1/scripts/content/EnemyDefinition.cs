using System;
using Godot;

namespace Game1;

/// <summary>普通敌军的只读数值与职责配置；运行时实例只消费，不改写。</summary>
[GlobalClass]
public partial class EnemyDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public BehaviorId Behavior { get; set; }
    [Export] public EnemyMovementMode MovementMode { get; set; }
    [Export] public int Armor { get; set; } = 20;
    [Export] public float MoveSpeed { get; set; } = 42f;
    [Export] public float AttackRange { get; set; } = 100f;
    [Export] public float RetreatRange { get; set; }
    [Export] public float AttackCooldown { get; set; } = 1.35f;
    [Export] public float TelegraphSeconds { get; set; } = 0.35f;
    [Export] public int Damage { get; set; } = 9;
    [Export] public float ProjectileSpeed { get; set; } = 190f;
    [Export] public float VisualScale { get; set; } = 0.5f;
    [Export] public Texture2D Texture { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || !string.Equals(Id, Id.Trim(), StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(DisplayName) || !Enum.IsDefined(Behavior) || !Enum.IsDefined(MovementMode) ||
            Armor <= 0 || !float.IsFinite(MoveSpeed) || MoveSpeed <= 0f ||
            !float.IsFinite(AttackRange) || AttackRange <= 0f ||
            !float.IsFinite(RetreatRange) || RetreatRange < 0f || RetreatRange >= AttackRange ||
            !float.IsFinite(AttackCooldown) || AttackCooldown <= 0f ||
            !float.IsFinite(TelegraphSeconds) || TelegraphSeconds <= 0f ||
            Damage <= 0 || !float.IsFinite(ProjectileSpeed) || ProjectileSpeed <= 0f ||
            !float.IsFinite(VisualScale) || VisualScale <= 0f || Texture is null)
            throw new ArgumentException($"敌军定义 '{Id}' 包含无效字段。");
    }
}
