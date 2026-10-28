using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;

namespace Content.Server._Persistence14.Shuttles;

public sealed partial class SimpleStationAnchorSystem : EntitySystem
{
    [Dependency] private ShuttleSystem _shuttles = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(EntityUid uid, SimpleStationAnchorComponent comp, ref MapInitEvent args)
    {
        UpdateStatus((uid, comp));
    }

    /// <summary>
    /// Prevent unanchoring if told to by the component.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnanchorAttempt(EntityUid uid, SimpleStationAnchorComponent comp, ref UnanchorAttemptEvent args)
    {
        if (!comp.PreventUnanchoring)
            return;

        _popup.PopupEntity(
            Loc.GetString("station-anchor-unanchoring-failed"),
            uid,
            args.User,
            PopupType.Medium);

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnAnchorStatusChange(EntityUid uid, SimpleStationAnchorComponent comp, ref AnchorStateChangedEvent args)
    {
        UpdateStatus((uid, comp));
    }

    private void UpdateStatus(Entity<SimpleStationAnchorComponent> ent)
    {
        var xform = Transform(ent.Owner);
        var grid = xform.GridUid;
        var state = xform.Anchored ? StationAnchorState.Anchored : StationAnchorState.Unanchored;

        if (!grid.HasValue)
            return;

        var args = new StationAnchorAttemptEvent
        {
            Anchor = ent.Owner,
            Grid = grid.Value,
            State = state,
        };
        RaiseLocalEvent(ref args);

        if (args.Cancelled)
            return;

        switch (state)
        {
            case StationAnchorState.Anchored:
                _shuttles.Disable(grid.Value); // A disabled grid/shuttle is stuck in place.
                break;
            case StationAnchorState.Unanchored:
                _shuttles.Enable(grid.Value); // An enabled grid/shuttle can move around.
                break;
        }
    }
}

[ByRefEvent]
public sealed partial class StationAnchorAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The anchor entity causing the anchoring
    /// </summary>
    public required EntityUid Anchor;

    /// <summary>
    /// The grid that is being anchored/unanchored.
    /// </summary>
    public required EntityUid Grid;

    /// <summary>
    /// If true, the grid is being anchored.
    /// If false, the grid is being unanchored.
    /// </summary>
    public required StationAnchorState State;
}

public enum StationAnchorState
{
    Anchored,
    Unanchored
}