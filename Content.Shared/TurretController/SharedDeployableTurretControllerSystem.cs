using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Turrets;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.TurretController;

/// <summary>
/// Oversees entities that can change the component values of linked deployable turrets,
/// specifically their armament and access level exemptions, via an associated UI
/// </summary>
public abstract partial class SharedDeployableTurretControllerSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessreader = default!;
    [Dependency] private readonly TurretTargetSettingsSystem _turretTargetingSettings = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Handling of client messages
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeployableTurretArmamentSettingChangedMessage>(OnArmamentSettingChanged);
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeployableTurretExemptAccessAssignmentChangedMessage>(OnExemptAccessAssignmentsChanged);
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeployableTurretExemptAccessNameChangedMessage>(OnExemptAccessNameChanged);

    }

    private void OnArmamentSettingChanged(Entity<DeployableTurretControllerComponent> ent, ref DeployableTurretArmamentSettingChangedMessage args)
    {
        if (IsUserAllowedAccess(ent, args.Actor))
            ChangeArmamentSetting(ent, args.ArmamentState, args.Actor);

        if (_userInterfaceSystem.TryGetOpenUi(ent.Owner, DeployableTurretControllerUiKey.Key, out var bui))
            bui.Update<DeployableTurretControllerBoundInterfaceState>();
    }

    private void OnExemptAccessAssignmentsChanged(Entity<DeployableTurretControllerComponent> ent, ref DeployableTurretExemptAccessAssignmentChangedMessage args)
    {
        if (IsUserAllowedAccess(ent, args.Actor))
            ChangeExemptAccessAssignments(ent, args.AccessAssignments, args.Enabled, args.Actor);

        if (_userInterfaceSystem.TryGetOpenUi(ent.Owner, DeployableTurretControllerUiKey.Key, out var bui))
            bui.Update<DeployableTurretControllerBoundInterfaceState>();
    }

    private void OnExemptAccessNameChanged(Entity<DeployableTurretControllerComponent> ent, ref DeployableTurretExemptAccessNameChangedMessage args)
    {
        if (IsUserAllowedAccess(ent, args.Actor))
            ChangeExemptAccessName(ent, args.AccessNames, args.Delete, args.Actor);

        if (_userInterfaceSystem.TryGetOpenUi(ent.Owner, DeployableTurretControllerUiKey.Key, out var bui))
            bui.Update<DeployableTurretControllerBoundInterfaceState>();
    }

    protected virtual void ChangeArmamentSetting(Entity<DeployableTurretControllerComponent> ent, int armamentState, EntityUid? user = null)
    {
        ent.Comp.ArmamentState = armamentState;
        Dirty(ent);

        _appearance.SetData(ent, TurretControllerVisuals.ControlPanel, armamentState);

        // Linked turrets are updated on the server side
    }

    protected virtual void ChangeExemptAccessAssignments(
        Entity<DeployableTurretControllerComponent> ent,
        HashSet<int> exemptions,
        bool enabled,
        EntityUid? user = null
    )
    {
        // Update the controller
        if (!TryComp<TurretTargetSettingsComponent>(ent, out var targetSettings))
            return;

        var controller = new Entity<TurretTargetSettingsComponent>(ent, targetSettings);

        foreach (var accessAssignment in exemptions)
        {
            if (!ent.Comp.AccessAssignments.Contains(accessAssignment))
                continue;

            _turretTargetingSettings.SetAccessAssignment(controller, accessAssignment, enabled);
        }

        Dirty(controller);

        // Linked turrets are updated on the server side
    }

    protected virtual void ChangeExemptAccessName(
        Entity<DeployableTurretControllerComponent> ent,
        HashSet<string> exemptions,
        bool delete,
        EntityUid? user = null
    )
    {
        // Update the controller
        if (!TryComp<TurretTargetSettingsComponent>(ent, out var targetSettings))
            return;

        var controller = new Entity<TurretTargetSettingsComponent>(ent, targetSettings);

        foreach (var accessName in exemptions)
        {
            _turretTargetingSettings.SetAccessName(controller, accessName, delete);
        }

        Dirty(controller);

        // Linked turrets are updated on the server side
    }

    public bool IsUserAllowedAccess(Entity<DeployableTurretControllerComponent> ent, EntityUid user)
    {
        if (_accessreader.IsAllowed(user, ent))
            return true;

        _popup.PopupClient(Loc.GetString("turret-controls-access-denied"), ent, user);
        _audio.PlayPredicted(ent.Comp.AccessDeniedSound, ent, user);

        return false;
    }

    protected virtual void ClearExemptAccessAssignments(Entity<DeployableTurretControllerComponent> ent)
    {
         if (!TryComp<TurretTargetSettingsComponent>(ent, out var targetSettings))
            return;

        var controller = new Entity<TurretTargetSettingsComponent>(ent, targetSettings);
        _turretTargetingSettings.SetAccessAssignments(controller, targetSettings.ExemptAccessAssignments, false);
    }

    protected virtual void ClearExemptAccessNames(Entity<DeployableTurretControllerComponent> ent)
    {
         if (!TryComp<TurretTargetSettingsComponent>(ent, out var targetSettings))
            return;

        var controller = new Entity<TurretTargetSettingsComponent>(ent, targetSettings);
        _turretTargetingSettings.SetAccessNames(controller, targetSettings.ExemptAccessNames, false);
    }
}
