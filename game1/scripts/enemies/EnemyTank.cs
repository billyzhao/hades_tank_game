using Godot;
using System;
using System.Collections.Generic;

namespace Game1;

/// <summary>可读的灰盒敌人：出生预警后才移动；Alpha 02B 起所有职责只攻击玩家。</summary>
public partial class EnemyTank : CharacterBody2D, ITeamDamageable
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    private static readonly Texture2D PatrolTexture = GD.Load<Texture2D>("res://assets/sprites/enemies/patrol_tank.png");
    private static readonly Texture2D AssaultTexture = GD.Load<Texture2D>("res://assets/sprites/enemies/assault_vehicle.png");
    private static readonly Texture2D MortarTexture = GD.Load<Texture2D>("res://assets/sprites/enemies/siege_tank.png");
    private static readonly Texture2D EliteTexture = GD.Load<Texture2D>("res://assets/sprites/enemies/elite_tank.png");
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
    public Team DamageTeam => Team.Enemy;

    public void SetPathProvider(IEnemyPathProvider pathProvider) => _pathProvider = pathProvider;

    public override void _Ready()
    {
        // 重启脉冲和房间清理只面向普通敌军；由实体自注册可覆盖场景预置与运行时生成两种来源。
        AddToGroup("enemies");
        _visual = GetNode<Polygon2D>("Visual");
        _roleSprite = GetNode<Sprite2D>("RoleSprite");
        _roleSprite.Texture = SelectRuntimeTexture();
        _attackWarning = GetNode<Line2D>("AttackWarning");
        _telegraphRemaining = Behavior switch { BehaviorId.Scout => 0.12f, BehaviorId.Assault => 0.2f, BehaviorId.Mortar => 0.75f, _ => TelegraphSeconds };
        SetVisualColor(EnemyVisualPalette.GetRoleTint(Behavior));
        _attackWarning.DefaultColor = EnemyVisualPalette.GetRoleTint(Behavior);
        _attackWarning.Visible = false;
        _baseVisualScale = Behavior == BehaviorId.Mortar ? 0.62f : Behavior == BehaviorId.Scout ? 0.38f : 0.50f;
        MoveSpeed = Behavior switch { BehaviorId.Scout => Math.Max(MoveSpeed, 68f), BehaviorId.Assault => Math.Max(MoveSpeed, 54f), BehaviorId.Mortar => Math.Min(MoveSpeed, 36f), _ => MoveSpeed };
        _roleSprite.Scale = Vector2.One * _baseVisualScale;
        if (IsEliteVisual) _eliteCycle = 1.25f;
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
                _attackCooldown = 1.3f;
                _attackWarning.Visible = false;
                SetVisualColor(EnemyVisualPalette.GetRoleTint(Behavior));
            }
            return;
        }
        float attackRange = Behavior == BehaviorId.Mortar ? 145f : Behavior == BehaviorId.Scout ? 72f : 95f;
        if (_attackCooldown <= 0f && GlobalPosition.DistanceTo(targetNode.GlobalPosition) < attackRange)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            _attackTelegraphRemaining = Behavior switch { BehaviorId.Scout => 0.16f, BehaviorId.Assault => 0.2f, BehaviorId.Mortar => 0.75f, _ => 0.35f };
            _attackWarning.Visible = true;
            return;
        }
        _attackWarning.Visible = false;
        _repathRemaining -= (float)delta;
        if (_pathProvider is not null && _repathRemaining <= 0f)
        {
            _path = _pathProvider.GetWorldPath(GlobalPosition, targetNode.GlobalPosition);
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
        Vector2 nextPoint = _pathProvider is null ? targetNode.GlobalPosition : _path[_pathIndex];
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
        int damage = Behavior switch { BehaviorId.Scout => 5, BehaviorId.Mortar => 14, _ => 10 };
        float speed = Behavior == BehaviorId.Mortar ? 135f : 190f;
        projectile.Initialize(new ProjectileSpec(damage, speed, 1.7f, 0), Team.Enemy, direction);
        EmitSignal(SignalName.ProjectileFired);
    }

    private void ApplyMovementPose(float delta, bool moving)
    {
        if (moving) _motionClock += delta;
        EnemyMotionPose pose = EnemyMotionVisual.Calculate(_motionClock, moving ? MoveSpeed : 0f, _baseVisualScale);
        _roleSprite.Position = new Vector2(0f, pose.LateralOffset);
        _roleSprite.Scale = pose.Scale;
    }

    private Texture2D SelectRuntimeTexture()
    {
        if (IsEliteVisual) return EliteTexture;
        return Behavior switch
        {
            BehaviorId.Assault => AssaultTexture,
            BehaviorId.Mortar => MortarTexture,
            _ => PatrolTexture
        };
    }

    private float GetEffectiveMoveSpeed(float delta)
    {
        if (!IsEliteVisual) return MoveSpeed;
        _eliteCycle -= delta;
        if (_eliteCycle <= 0f) _eliteCycle = 2.0f;
        // 周期前 1.25 秒冲刺追击，后 0.75 秒明显冷却，为玩家提供可读反击窗口。
        bool overdrive = _eliteCycle > 0.75f;
        _roleSprite.Modulate = overdrive ? new Color(1f, 0.72f, 0.18f) : new Color(0.55f, 0.55f, 0.55f);
        return MoveSpeed * (overdrive ? 1.55f : 0.55f);
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
