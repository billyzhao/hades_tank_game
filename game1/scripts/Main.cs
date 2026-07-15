using Godot;

public partial class Main : Node2D
{
    private static readonly Vector2 PlayerSize = new(34.0f, 34.0f);
    private static readonly Vector2 TargetSize = new(24.0f, 24.0f);
    private const float TargetMargin = 40.0f;

    [Export]
    public float PlayerSpeed { get; set; } = 320.0f;

    private ColorRect _player = null!;
    private ColorRect _target = null!;
    private Label _scoreLabel = null!;
    private int _score;
    private Vector2 _playfield = new(960.0f, 540.0f);

    public override void _Ready()
    {
        _player = GetNode<ColorRect>("Player");
        _target = GetNode<ColorRect>("Target");
        _scoreLabel = GetNode<Label>("UI/ScoreLabel");

        _playfield = GetViewportRect().Size;
        _player.Size = PlayerSize;
        _target.Size = TargetSize;
        _player.Position = _playfield * 0.5f - PlayerSize * 0.5f;

        RespawnTarget();
        UpdateScoreLabel();
    }

    public override void _Process(double delta)
    {
        MovePlayer((float)delta);
        CollectTargetIfTouching();
    }

    private void MovePlayer(float delta)
    {
        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        _player.Position += direction * PlayerSpeed * delta;
        _player.Position = new Vector2(
            Mathf.Clamp(_player.Position.X, 0.0f, _playfield.X - PlayerSize.X),
            Mathf.Clamp(_player.Position.Y, 0.0f, _playfield.Y - PlayerSize.Y)
        );
    }

    private void CollectTargetIfTouching()
    {
        if (!_player.GetRect().Intersects(_target.GetRect()))
        {
            return;
        }

        _score = GameRules.NextScore(_score);
        RespawnTarget();
        UpdateScoreLabel();
    }

    private void RespawnTarget()
    {
        Vector2 center = GameRules.TargetPositionForScore(_score, _playfield, TargetMargin);
        _target.Position = center - TargetSize * 0.5f;
    }

    private void UpdateScoreLabel()
    {
        _scoreLabel.Text = $"Score: {_score}";
    }
}
