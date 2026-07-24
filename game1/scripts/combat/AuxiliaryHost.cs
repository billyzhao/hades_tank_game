using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

#nullable enable

namespace Game1;

/// <summary>玩家坦克的两个自动辅助槽。它只消费 BuildController 快照，不持有 RunState 可写引用。</summary>
public partial class AuxiliaryHost : Node, IAuxiliaryRuntime
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    private static readonly IReadOnlyDictionary<string, Texture2D> VisualTextures =
        new Dictionary<string, Texture2D>(StringComparer.Ordinal)
        {
            ["aux_orbit_drone"] = GD.Load<Texture2D>("res://assets/sprites/auxiliaries/orbit_drone.png"),
            ["aux_side_cannon"] = GD.Load<Texture2D>("res://assets/sprites/auxiliaries/side_cannon.png"),
            ["aux_mine_layer"] = GD.Load<Texture2D>("res://assets/sprites/auxiliaries/mine_layer.png"),
            ["aux_suppression_field"] = GD.Load<Texture2D>("res://assets/sprites/auxiliaries/suppression_field.png")
        };
    private readonly Dictionary<string, AuxiliaryDefinition> _definitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Sprite2D> _slotVisuals = new(StringComparer.Ordinal);
    private BuildController? _build;
    private Node2D? _owner;
    private readonly Dictionary<string, float> _cooldowns = new(StringComparer.Ordinal);
    private Vector2 _lastPosition;
    private float _travelled;
    private float _visualPhase;

    public string AuxiliaryId { get; private set; } = string.Empty;

    public void AttachBuild(BuildController build, ContentCatalog catalog)
    {
        _build = build ?? throw new ArgumentNullException(nameof(build));
        _definitions.Clear();
        foreach (AuxiliaryDefinition definition in catalog.Auxiliaries)
            _definitions.Add(definition.Id, definition);
        _build.SnapshotChanged += Refresh;
        Refresh();
    }

    public override void _ExitTree() => Deactivate();

    public override void _PhysicsProcess(double delta)
    {
        if (_owner is null || GetTree().Paused) return;
        _travelled += _owner.GlobalPosition.DistanceTo(_lastPosition);
        _lastPosition = _owner.GlobalPosition;
        IReadOnlyList<AuxiliarySlotState> slots = _build?.GetSnapshot().AuxiliarySlots ?? Array.Empty<AuxiliarySlotState>();
        foreach (AuxiliarySlotState slot in slots)
        {
            if (!_definitions.TryGetValue(slot.AuxiliaryId, out AuxiliaryDefinition? definition)) continue;
            float cooldown = Mathf.Max(0f, _cooldowns.GetValueOrDefault(slot.AuxiliaryId) - (float)delta);
            _cooldowns[slot.AuxiliaryId] = cooldown;
            if (cooldown > 0f) continue;
            if (definition.TargetMode == AuxiliaryTargetMode.MovementDistance)
            {
                if (_travelled < 42f) continue;
                _travelled = 0f;
            }
            Node2D? target = GetNearestEnemy(definition.Range);
            if (definition.TargetMode == AuxiliaryTargetMode.AreaDensity) ApplyAreaSuppression(definition, slot.Rank);
            else if (target is not null) FireAt(target.GlobalPosition, definition, slot.Rank);
            else continue;
            _cooldowns[slot.AuxiliaryId] = definition.BaseCooldown / Mathf.Max(0.35f, 1f + 0.16f * (slot.Rank - 1));
        }
    }

    public override void _Process(double delta)
    {
        if (_slotVisuals.Count == 0) return;
        _visualPhase += (float)delta;
        int index = 0;
        foreach (Sprite2D visual in _slotVisuals.Values)
        {
            float baseAngle = index == 0 ? Mathf.Pi : 0f;
            float angle = baseAngle + Mathf.Sin(_visualPhase * 1.8f + index) * .18f;
            visual.Position = Vector2.FromAngle(angle) * 15f;
            visual.Rotation = angle + Mathf.Pi / 2f;
            index++;
        }
    }

    public void Configure(AuxiliaryDefinition definition, BuildSnapshot build)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Id] = definition;
        AuxiliaryId = build.AuxiliarySlots.Any(slot => slot.AuxiliaryId == definition.Id) ? definition.Id : string.Empty;
    }

    public void Activate(Node2D owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _lastPosition = owner.GlobalPosition;
        SetPhysicsProcess(true);
    }

    public void Deactivate()
    {
        if (_build is not null) _build.SnapshotChanged -= Refresh;
        _owner = null;
        _cooldowns.Clear();
        ClearSlotVisuals();
        SetPhysicsProcess(false);
    }

    private void Refresh()
    {
        if (_build is null) return;
        IReadOnlyList<AuxiliarySlotState> slots = _build.GetSnapshot().AuxiliarySlots;
        AuxiliarySlotState? first = slots.FirstOrDefault();
        AuxiliaryId = first?.AuxiliaryId ?? string.Empty;
        ClearSlotVisuals();
        foreach (AuxiliarySlotState slot in slots.Take(2))
        {
            if (!VisualTextures.TryGetValue(slot.AuxiliaryId, out Texture2D? texture)) continue;
            Sprite2D visual = new()
            {
                Name = $"Visual_{slot.AuxiliaryId}",
                Texture = texture,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                ZIndex = 3
            };
            AddChild(visual);
            _slotVisuals[slot.AuxiliaryId] = visual;
        }
    }

    private void ClearSlotVisuals()
    {
        foreach (Sprite2D visual in _slotVisuals.Values)
        {
            if (IsInstanceValid(visual)) visual.QueueFree();
        }
        _slotVisuals.Clear();
    }

    private Node2D? GetNearestEnemy(float range)
    {
        if (_owner is null) return null;
        return GetTree().GetNodesInGroup("enemies")
            .OfType<Node2D>()
            .Where(enemy => IsInstanceValid(enemy) && enemy.GlobalPosition.DistanceTo(_owner.GlobalPosition) <= range)
            .OrderBy(enemy => enemy.GlobalPosition.DistanceSquaredTo(_owner.GlobalPosition))
            .FirstOrDefault();
    }

    private void FireAt(Vector2 target, AuxiliaryDefinition definition, int rank)
    {
        if (_owner is null) return;
        Vector2 direction = (target - _owner.GlobalPosition).Normalized();
        if (direction.IsZeroApprox()) return;
        SpriteEffectPlayer.Spawn(
            GetTree().CurrentScene,
            _owner.GlobalPosition + direction * 12f,
            ArtTextureCatalog.MuzzleFlash,
            18f,
            .34f,
            11,
            new Color(.35f, .9f, 1f));
        Projectile projectile = ProjectileScene.Instantiate<Projectile>();
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = _owner.GlobalPosition + direction * 11f;
        projectile.Initialize(new ProjectileSpec(definition.BaseDamage + (rank - 1) * 3, 230f, 1.25f, 0), Team.Player, direction);
    }

    private void ApplyAreaSuppression(AuxiliaryDefinition definition, int rank)
    {
        if (_owner is null) return;
        // 区域压制仍复用正式地面环序列，但采用青色友军语义；表现不参与范围或伤害计算。
        SpriteEffectPlayer.Spawn(
            GetTree().CurrentScene,
            _owner.GlobalPosition,
            ArtTextureCatalog.MortarWarning,
            12f,
            .85f,
            8,
            new Color(.24f, .9f, 1f, .72f));
        foreach (Node2D target in GetTree().GetNodesInGroup("enemies").OfType<Node2D>())
        {
            if (target.GlobalPosition.DistanceTo(_owner.GlobalPosition) <= definition.Range && target is ITeamDamageable damageable)
                damageable.ApplyDamage(new DamageContext(definition.BaseDamage + rank * 2));
        }
    }
}
