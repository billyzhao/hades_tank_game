using System;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>三选一奖励的可见入口；只报告选择，不直接写入本局状态。</summary>
public partial class RewardPanel : PanelContainer
{
    public event Action<string> ProtocolChosen;
    private readonly Button[] _cards = new Button[3];
    private string[] _ids = Array.Empty<string>();

    public override void _Ready()
    {
        Visible = false;
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, CustomMinimumSize = new Vector2(420, 72) };
        AddChild(row);
        for (int index = 0; index < _cards.Length; index++)
        {
            int captured = index;
            _cards[index] = new Button { CustomMinimumSize = new Vector2(132, 68), AutowrapMode = TextServer.AutowrapMode.WordSmart };
            _cards[index].Pressed += () => Choose(captured);
            row.AddChild(_cards[index]);
        }
    }

    public void ShowOffer(ProtocolOffer offer, ContentCatalog catalog)
    {
        _ids = offer.ProtocolIds.ToArray();
        for (int index = 0; index < _ids.Length; index++)
        {
            ProtocolDefinition protocol = catalog.GetProtocol(_ids[index]);
            _cards[index].Text = $"[{index + 1}] {protocol.DisplayName}\n{protocol.Description}";
            _cards[index].Disabled = false;
        }
        Visible = true;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!Visible || @event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key3) Choose((int)(key.Keycode - Key.Key1));
    }

    private void Choose(int index)
    {
        if (!Visible || index < 0 || index >= _ids.Length) return;
        Visible = false;
        ProtocolChosen?.Invoke(_ids[index]);
    }
}
