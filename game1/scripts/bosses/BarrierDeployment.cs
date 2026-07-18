using Godot;

namespace Game1;

/// <summary>路障部署的房间级挂载点；07B 后续步骤在此集中处理预警、合法格校验和运行时 TileMap 写入。</summary>
public partial class BarrierDeployment : Node
{
    private const float DefaultPreviewSeconds = .8f;
    private TileMapLayer _structure;
    private Node2D _player;
    private Node2D _relay;
    private int _cellSize;
    private System.Action _navigationRefresh;
    private Polygon2D _preview;
    private bool _previewing;

    /// <summary>由遭遇编排器注入房间实例对象；不保存或修改任何静态 TileSet 资源。</summary>
    public void Configure(TileMapLayer structure, Node2D player, Node2D relay, int cellSize, System.Action navigationRefresh = null)
    {
        _structure = structure ?? throw new System.ArgumentNullException(nameof(structure));
        _player = player ?? throw new System.ArgumentNullException(nameof(player));
        _relay = relay ?? throw new System.ArgumentNullException(nameof(relay));
        if (cellSize <= 0) throw new System.ArgumentOutOfRangeException(nameof(cellSize));
        _cellSize = cellSize;
        _navigationRefresh = navigationRefresh;
    }

    /// <summary>只接受空格，且不能覆盖玩家或中继站所处的格子。</summary>
    public bool IsLegalCell(Vector2I cell)
    {
        if (_structure is null || _player is null || _relay is null || _cellSize <= 0) return false;
        if (_structure.GetCellSourceId(cell) != -1) return false;

        Vector2 center = new((cell.X + .5f) * _cellSize, (cell.Y + .5f) * _cellSize);
        float protectedRadius = _cellSize * .7f;
        return center.DistanceTo(_player.GlobalPosition) > protectedRadius
            && center.DistanceTo(_relay.GlobalPosition) > protectedRadius;
    }

    /// <summary>仅写入当前房间实例的 Structure 层；调用方可在此后重建共享导航网格。</summary>
    public bool DeployNow(Vector2I cell)
    {
        if (!IsLegalCell(cell)) return false;
        _structure.SetCell(cell, 0, Vector2I.Zero);
        _navigationRefresh?.Invoke();
        return true;
    }

    /// <summary>先以半透明红框标记落点，再在预警结束后写入当前房间实例的 Structure。</summary>
    public async void PreviewAndDeploy(Vector2I cell, float previewSeconds = DefaultPreviewSeconds)
    {
        if (_previewing || !IsLegalCell(cell) || !IsInsideTree()) return;
        _previewing = true;
        EnsurePreview();
        _preview.Position = new Vector2((cell.X + .5f) * _cellSize, (cell.Y + .5f) * _cellSize);
        _preview.Visible = true;
        await ToSignal(GetTree().CreateTimer(Mathf.Max(.05f, previewSeconds)), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree())
        {
            DeployNow(cell);
            _preview.Visible = false;
        }
        _previewing = false;
    }

    private void EnsurePreview()
    {
        if (_preview is not null) return;
        float half = _cellSize * .46f;
        _preview = new Polygon2D
        {
            Name = "BarrierPreview",
            Polygon = new Vector2[] { new(-half, -half), new(half, -half), new(half, half), new(-half, half) },
            Color = new Color(1f, .16f, .08f, .55f),
            ZIndex = 6,
            Visible = false
        };
        AddChild(_preview);
    }
}
