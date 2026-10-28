using Content.Shared.TurretController;
using Robust.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using System.Linq;

namespace Content.Client.TurretController;

public sealed class TurretControllerWindowBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private TurretControllerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TurretControllerWindow>();
        _window.SetOwner(Owner);
        _window.OpenCentered();
        _window.OnAccessAssignmentsChangedEvent += OnAccessAssignmentsChanged;
        _window.OnAccessNameChangedEvent += OnAccessNameChanged;
        _window.OnArmamentSettingChangedEvent += OnArmamentSettingChanged;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DeployableTurretControllerBoundInterfaceState { } castState)
            return;

        _window?.UpdateState(castState);
    }

    private void OnAccessAssignmentsChanged(HashSet<int> accessAssignments, bool enabled)
    {
        SendPredictedMessage(new DeployableTurretExemptAccessAssignmentChangedMessage(accessAssignments, enabled));
    }

    private void OnAccessNameChanged(HashSet<string> accessNames, bool delete)
    {
        SendPredictedMessage(new DeployableTurretExemptAccessNameChangedMessage(accessNames, delete));
    }

    private void OnArmamentSettingChanged(TurretControllerWindow.TurretArmamentSetting setting)
    {
        SendPredictedMessage(new DeployableTurretArmamentSettingChangedMessage((int)setting));
    }
}
