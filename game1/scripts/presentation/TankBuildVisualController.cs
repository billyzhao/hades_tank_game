using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>
/// 把只读构筑快照投影为坦克外观。它不识别协议 Id，不参与属性、奖励或碰撞结算。
/// </summary>
public partial class TankBuildVisualController : Node
{
    private readonly Dictionary<ProtocolDepartment, Sprite2D> _modules = new();
    private readonly Dictionary<ProtocolDepartment, int> _scores = new();
    private PlayerTank _player = null!;
    private Node2D _turret = null!;
    private BuildController _build = null!;
    private TankBuildVisualCatalog _visualCatalog = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _player = GetParent<PlayerTank>();
        _turret = _player.GetNode<Node2D>("Turret");
    }

    public void AttachBuild(BuildController build, TankBuildVisualCatalog visualCatalog)
    {
        _build = build ?? throw new ArgumentNullException(nameof(build));
        _visualCatalog = visualCatalog ?? throw new ArgumentNullException(nameof(visualCatalog));
        _visualCatalog.Validate(_build.Catalog);
        _build.SnapshotChanged += Refresh;
        Refresh();
    }

    public void PlayHitFlash()
    {
        foreach (Sprite2D module in _modules.Values)
        {
            if (!IsInstanceValid(module)) continue;
            module.Modulate = new Color(1f, .42f, .24f);
            module.CreateTween().TweenProperty(module, "modulate", Colors.White, .13f);
        }
    }

    public override void _ExitTree()
    {
        if (_build is not null) _build.SnapshotChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_build is null || _visualCatalog is null) return;
        BuildSnapshot snapshot = _build.GetSnapshot();
        Dictionary<ProtocolDepartment, int> currentScores = snapshot.OwnedProtocols
            .GroupBy(owned => _build.Catalog.GetProtocol(owned.ProtocolId).Department)
            .ToDictionary(group => group.Key, group => group.Sum(item => (int)item.Rank));

        foreach (ProtocolDepartmentVisualDefinition definition in _visualCatalog.ProtocolVisuals)
        {
            int score = currentScores.GetValueOrDefault(definition.Department);
            if (score <= 0)
            {
                if (_modules.Remove(definition.Department, out Sprite2D removed) && IsInstanceValid(removed))
                    removed.QueueFree();
                _scores.Remove(definition.Department);
                continue;
            }

            Sprite2D module = GetOrCreate(definition);
            int stage = Math.Clamp(score, 1, 3);
            Vector2 targetScale = Vector2.One * definition.BaseScale * (1f + .16f * (stage - 1));
            bool changed = _scores.GetValueOrDefault(definition.Department) != score;
            _scores[definition.Department] = score;
            module.Texture = definition.Texture;
            module.Position = definition.LocalPosition;
            module.RotationDegrees = definition.RotationDegrees;
            module.ZIndex = definition.Slot is TankVisualSlot.TurretCenter or TankVisualSlot.TurretTop ? 3 : 2;
            module.Modulate = Colors.White;

            if (changed)
                PlayAssembly(module, targetScale, definition.AccentColor, stage);
            else
                module.Scale = targetScale;
        }
    }

    private Sprite2D GetOrCreate(ProtocolDepartmentVisualDefinition definition)
    {
        if (_modules.TryGetValue(definition.Department, out Sprite2D existing) && IsInstanceValid(existing))
            return existing;

        Sprite2D module = new()
        {
            Name = $"ProtocolModule_{definition.Department}",
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Centered = true
        };
        ParentFor(definition.Slot).AddChild(module);
        _modules[definition.Department] = module;
        return module;
    }

    private Node2D ParentFor(TankVisualSlot slot) => slot switch
    {
        TankVisualSlot.TurretCenter or TankVisualSlot.TurretTop => _turret,
        TankVisualSlot.BodyCenter or TankVisualSlot.BodyRear => _player,
        _ => throw new ArgumentOutOfRangeException(nameof(slot))
    };

    private void PlayAssembly(Sprite2D module, Vector2 targetScale, Color accent, int stage)
    {
        module.Scale = targetScale * .45f;
        module.Modulate = new Color(accent.R, accent.G, accent.B, .25f);
        Tween tween = module.CreateTween().SetParallel();
        tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(module, "scale", targetScale, .34f);
        tween.TweenProperty(module, "modulate", Colors.White, .24f);
        SpriteEffectPlayer.Spawn(
            GetTree().CurrentScene,
            _player.GlobalPosition,
            ArtTextureCatalog.LevelUp,
            15f,
            .62f + stage * .08f,
            24,
            accent);
    }
}
