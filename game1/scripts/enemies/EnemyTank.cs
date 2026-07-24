using Godot;
using System;
using System.Collections.Generic;

namespace Game1;

/// <summary>可读的灰盒敌人：出生预警后才移动；Alpha 02B 起所有职责只攻击玩家。</summary>
public partial class EnemyTank : CharacterBody2D, ITeamDamageable
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    private static readonly Texture2D EliteTexture = GD.Load<Texture2D>("res://assets/sprites/enemies/elite_tank.png");
    private static readonly ContentCatalog EnemyCatalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
    [Export] public BehaviorId Behavior { get; set; } = BehaviorId.Patrol;
    [Export] public float MoveSpeed { get; set; } = 42f;
    [Export] public float TelegraphSeconds { get; set; } = 0.35f;
    [Export] public int Armor { get; set; } = 20;
    /// <summary>精英仅附加“过载加速—冷却减速”一条规则，不用生命膨胀堆难度。</summary>
    public bool IsEliteVisual { get; set; }
    [Signal] public delegate void DestroyedEventHandler();
    [Signal] public delegate void ProjectileFiredEventHandler();
    private float _telegraphRemaining;
    private float _attackTelegraphRemaining;
    private float _attackCooldown;
    private Polygon2D _visual = null!;
    private Sprite2D _roleSprite = null!;
    private Line2D _attackWarning = null!;
    private float _baseVisualScale;
    private float _motionClock;
    private IEnemyPathProvider _pathProvider;
    private IReadOnlyList<Vector2> _path = System.Array.Empty<Vector2>();
    private int _pathIndex;
    private float _repathRemaining;
    private float _eliteCycle;
    private float _movementClock;
    private EnemyDefinition _definition;
    private EliteModifierDefinition _eliteModifier;
    public Team DamageTeam => Team.Enemy;

    public void SetPathProvider(IEnemyPathProvider pathProvider) => _pathProvider = pathProvider;

    public void Configure(EnemyDefinition definition, EliteModifierDefinition eliteModifier = null)
    {
        if (IsInsideTree()) throw new InvalidOperationException("敌军职责必须在加入场景树前配置。");
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _definition.Validate();
        eliteModifier?.Validate();
        _eliteModifier = eliteModifier;
        IsEliteVisual = eliteModifier is not null;
        Behavior = _definition.Behavior;
        MoveSpeed = _definition.MoveSpeed;
        TelegraphSeconds = _definition.TelegraphSeconds;
        Armor = _definition.Armor;
    }

    public override void _Ready()
    {
        // 重启脉冲和房间清理只面向普通敌军；由实体自注册可覆盖场景预置与运行时生成两种来源。
        AddToGroup("enemies");
        _visual = GetNode<Polygon2D>("Visual");
        _roleSprite = GetNode<Sprite2D>("RoleSprite");
        _definition ??= EnemyCatalog.GetEnemy(Behavior);
        _definition.Validate();
        Behavior = _definition.Behavior;
        MoveSpeed = _definition.MoveSpeed;
        TelegraphSeconds = _definition.TelegraphSeconds;
        Armor = _definition.Armor;
        _roleSprite.Texture = IsEliteVisual ? EliteTexture : _definition.Texture;
        _attackWarning = GetNode<Line2D>("AttackWarning");
        _telegraphRemaining = TelegraphSeconds;
        SetVisualColor(EnemyVisualPalette.GetRoleTint(Behavior));
        _attackWarning.DefaultColor = EnemyVisualPalette.GetRoleTint(Behavior);
        _attackWarning.Visible = false;
        _baseVisualScale = _definition.VisualScale;
        _roleSprite.Scale = Vector2.One * _baseVisualScale;
        if (IsEliteVisual)
        {
            _eliteModifier ??= EnemyCatalog.EliteModifiers[0];
            _eliteModifier.Validate();
            _eliteCycle = _eliteModifier.BoostSeconds + _eliteModifier.RecoverySeconds;
            Armor = Mathf.RoundToInt(Armor * _eliteModifier.ArmorMultiplier);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_telegraphRemaining > 0f)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            _telegraphRemaining -= (float)delta;
            SetVisualVisible(Mathf.Sin(_telegraphRemaining * 28f) > 0f);
            return;
        }
        SetVisualVisible(true);
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        TargetId target = TargetPolicy.SelectTarget(Behavior, new TargetSnapshot(player is not null));
        Node2D targetNode = target == TargetId.Player ? player : null;
        if (targetNode is null)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            return;
        }
        _movementClock += (float)delta;
        float targetDistance = GlobalPosition.DistanceTo(targetNode.GlobalPosition);
        EnemyMovementIntent movement = EnemyMovementPolicy.Calculate(
            _definition.MovementMode,
            GlobalPosition,
            targetNode.GlobalPosition,
            _definition.AttackRange,
            _definition.RetreatRange,
            _movementClock);
        _attackCooldown = Mathf.Max(0f, _attackCooldown - (float)delta);
        if (_attackTelegraphRemaining > 0f)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            _attackWarning.Visible = true;
            _attackTelegraphRemaining -= (float)delta;
            SetVisualColor(new Color(1f, 1f, 1f));
            if (_attackTelegraphRemaining <= 0f)
            {
                FireAt(targetNode);
                _attackCooldown = _definition.AttackCooldown;
                _attackWarning.Visible = false;
                SetVisualColor(EnemyVisualPalette.GetRoleTint(Behavior));
            }
            return;
        }
        bool mustRetreat = _definition.MovementMode == EnemyMovementMode.StandOff &&
                           targetDistance < _definition.RetreatRange;
        if (!mustRetreat && _attackCooldown <= 0f && targetDistance < _definition.AttackRange)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            _attackTelegraphRemaining = _definition.TelegraphSeconds;
            _attackWarning.Visible = true;
            return;
        }
        _attackWarning.Visible = false;
        if (!movement.ShouldMove)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            return;
        }
        _repathRemaining -= (float)delta;
        if (_pathProvider is not null && _repathRemaining <= 0f)
        {
            _path = _pathProvider.GetWorldPath(GlobalPosition, movement.Destination);
            _pathIndex = _path.Count > 1 ? 1 : 0;
            _repathRemaining = 0.25f;
        }
        if (_pathProvider is not null && _path.Count == 0)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            return;
        }
        while (_pathProvider is not null && _pathIndex < _path.Count - 1 && GlobalPosition.DistanceTo(_path[_pathIndex]) < 10f) _pathIndex++;
        Vector2 nextPoint = _pathProvider is null ? movement.Destination : _path[_pathIndex];
        Velocity = GlobalPosition.DirectionTo(nextPoint) * GetEffectiveMoveSpeed((float)delta);
        if (!Velocity.IsZeroApprox()) Rotation = Velocity.Angle();
        MoveAndSlide();
        ApplyMovementPose((float)delta, !GetRealVelocity().IsZeroApprox());
    }

    private void FireAt(Node2D targetNode)
    {
        Vector2 direction = GlobalPosition.DirectionTo(targetNode.GlobalPosition);
        if (direction.IsZeroApprox()) return;

        Projectile projectile = ProjectileScene.Instantiate<Projectile>();
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition + direction * 12f;
        projectile.Initialize(new ProjectileSpec(_definition.Damage, _definition.ProjectileSpeed, 1.7f, 0), Team.Enemy, direction);
        EmitSignal(SignalName.ProjectileFired);
    }

    private void ApplyMovementPose(float delta, bool moving)
    {
        if (moving) _motionClock += delta;
        EnemyMotionPose pose = EnemyMotionVisual.Calculate(_motionClock, moving ? MoveSpeed : 0f, _baseVisualScale);
        _roleSprite.Position = new Vector2(0f, pose.LateralOffset);
        _roleSprite.Scale = pose.Scale;
    }

    private float GetEffectiveMoveSpeed(float delta)
    {
        if (!IsEliteVisual) return MoveSpeed;
        _eliteCycle -= delta;
        float cycleSeconds = _eliteModifier.BoostSeconds + _eliteModifier.RecoverySeconds;
        if (_eliteCycle <= 0f) _eliteCycle = cycleSeconds;
        bool overdrive = _eliteCycle > _eliteModifier.RecoverySeconds;
        _roleSprite.Modulate = overdrive ? new Color(1f, 0.72f, 0.18f) : new Color(0.55f, 0.55f, 0.55f);
        return MoveSpeed * (overdrive ? _eliteModifier.BoostSpeedMultiplier : _eliteModifier.RecoverySpeedMultiplier);
    }

    private void SetVisualColor(Color color)
    {
        _visual.Color = color;
        _roleSprite.Modulate = color;
    }

    private void SetVisualVisible(bool visible)
    {
        _visual.Visible = visible;
        _roleSprite.Visible = visible;
    }

    public DamageResult ApplyDamage(DamageContext context)
    {
        int applied = System.Math.Min(Armor, System.Math.Max(0, context.Amount));
        bool destroyedNow = Armor > 0 && applied == Armor;
        Armor -= applied;
        if (applied > 0 && !destroyedNow) PlayHitFlash();
        if (destroyedNow)
        {
            EmitSignal(SignalName.Destroyed);
            QueueFree();
        }
        return new DamageResult(applied, destroyedNow);
    }

    public void ApplyRebootKnockback(Vector2 origin, float distance)
    {
        if (distance <= 0f) return;
        Vector2 direction = origin.DirectionTo(GlobalPosition);
        if (direction.IsZeroApprox()) direction = Vector2.Right;
        MoveAndCollide(direction * distance);
    }

    private async void PlayHitFlash()
    {
        SetVisualColor(Colors.White);
        await ToSignal(GetTree().CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree() && _attackTelegraphRemaining <= 0f)
        {
            SetVisualColor(EnemyVisualPalette.GetRoleTint(Behavior));
        }
    }
}
