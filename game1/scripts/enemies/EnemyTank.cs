using Godot;
using System.Collections.Generic;

namespace Game1;

/// <summary>可读的灰盒敌人：出生预警后才移动；Alpha 02B 起所有职责只攻击玩家。</summary>
public partial class EnemyTank : CharacterBody2D, IDamageable
{
    [Export] public BehaviorId Behavior { get; set; } = BehaviorId.Patrol;
    [Export] public float MoveSpeed { get; set; } = 42f;
    [Export] public float TelegraphSeconds { get; set; } = 0.35f;
    [Export] public int Armor { get; set; } = 20;
    [Signal] public delegate void DestroyedEventHandler();
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

    public void SetPathProvider(IEnemyPathProvider pathProvider) => _pathProvider = pathProvider;

    public override void _Ready()
    {
        // 重启脉冲和房间清理只面向普通敌军；由实体自注册可覆盖场景预置与运行时生成两种来源。
        AddToGroup("enemies");
        _visual = GetNode<Polygon2D>("Visual");
        _roleSprite = GetNode<Sprite2D>("RoleSprite");
        _attackWarning = GetNode<Line2D>("AttackWarning");
        _telegraphRemaining = Behavior switch { BehaviorId.Assault => 0.2f, BehaviorId.Siege => 0.75f, _ => TelegraphSeconds };
        SetVisualColor(EnemyVisualPalette.GetRoleTint(Behavior));
        _attackWarning.DefaultColor = EnemyVisualPalette.GetRoleTint(Behavior);
        _attackWarning.Visible = false;
        _baseVisualScale = Behavior == BehaviorId.Siege ? 0.62f : 0.50f;
        _roleSprite.Scale = Vector2.One * _baseVisualScale;
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
                if (target == TargetId.Player) player.GetNode<HealthComponent>("HealthComponent").ApplyDamage(new DamageContext(10));
                _attackCooldown = 1.3f;
                _attackWarning.Visible = false;
                SetVisualColor(EnemyVisualPalette.GetRoleTint(Behavior));
            }
            return;
        }
        if (_attackCooldown <= 0f && GlobalPosition.DistanceTo(targetNode.GlobalPosition) < 95f)
        {
            Velocity = Vector2.Zero;
            ApplyMovementPose((float)delta, false);
            _attackTelegraphRemaining = Behavior switch { BehaviorId.Assault => 0.2f, BehaviorId.Siege => 0.75f, _ => 0.35f };
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
        Velocity = GlobalPosition.DirectionTo(nextPoint) * MoveSpeed;
        if (!Velocity.IsZeroApprox()) Rotation = Velocity.Angle();
        MoveAndSlide();
        ApplyMovementPose((float)delta, !GetRealVelocity().IsZeroApprox());
    }

    private void ApplyMovementPose(float delta, bool moving)
    {
        if (moving) _motionClock += delta;
        EnemyMotionPose pose = EnemyMotionVisual.Calculate(_motionClock, moving ? MoveSpeed : 0f, _baseVisualScale);
        _roleSprite.Position = new Vector2(0f, pose.LateralOffset);
        _roleSprite.Scale = pose.Scale;
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
