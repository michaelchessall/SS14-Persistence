using Content.Client.CrewAssignments.UI;
using Content.Client.Eui;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.Eui;
using Content.Shared.Fax;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;

namespace Content.Client.CrewAssignments.AdminUI;

[UsedImplicitly]
public sealed class CodexEui : BaseEui
{
    private readonly CodexWindow _window;
    private readonly CodexEditMenu _edit;
    private string _sectorStatus = "";

    public CodexEui()
    {
        _window = new CodexWindow(this);
        _edit = new CodexEditMenu(this);
        _window.OnClose += () => SendMessage(new CodexEuiMsg.Close());
        _window.CreateButton.OnPressed += _ => OnCreate();
        _window.SectorButton.OnPressed += _ => OnSectorButton();
    }

    private void OnSectorButton()
    {
        _edit.UpdateState(-1, "Sector Status", _sectorStatus, new List<String>(), true);
        _edit.OpenCentered();
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not CodexEuiState cast)
            return;
        _window.CodexContainer.RemoveAllChildren();
        foreach (var entry in cast.Entries)
        {
            Button button = new();
            button.Text = entry.Title;
            button.MinHeight = 20;
            button.OnPressed += _ => OnSelect(entry.ID, entry.Title, entry.Description, entry.Whitelist, entry.Visible);
            _window.CodexContainer.AddChild(button);
            if (_edit._iD == entry.ID)
            {
                _edit.UpdateState(entry.ID, entry.Title, entry.Description, entry.Whitelist, entry.Visible);
            }
        }
        _sectorStatus = cast.SectorStatus;

    }

    public void OnCreate()
    {
        SendMessage(new CodexEuiMsg.CreateNew());
    }

    public void OnSelect(int iD, string title, string description, List<string> whitelist, bool visible)
    {
        _edit.UpdateState(iD, title, description, whitelist, visible);
        _edit.OpenCentered();
    }
}
