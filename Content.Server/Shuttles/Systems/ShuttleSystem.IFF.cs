using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private void InitializeIFF()
    {
        SubscribeLocalEvent<IFFConsoleComponent, AnchorStateChangedEvent>(OnIFFConsoleAnchor);
        SubscribeLocalEvent<IFFConsoleComponent, IFFShowIFFMessage>(OnIFFShow);
        SubscribeLocalEvent<IFFConsoleComponent, IFFSetColorMessage>(OnIFFSetColor);
        SubscribeLocalEvent<IFFConsoleComponent, IFFSetDesignationMessage>(OnIFFSetDesignation);
        SubscribeLocalEvent<IFFConsoleComponent, MapInitEvent>(OnInitIFFConsole);
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref GridSplitEvent ev)
    {
        var splitMass = _cfg.GetCVar(CCVars.HideSplitGridsUnder);

        if (splitMass < 0)
            return;

        foreach (var grid in ev.NewGrids)
        {
            if (!_physicsQuery.TryGetComponent(grid, out var physics) ||
                physics.Mass > splitMass)
            {
                continue;
            }

            AddIFFFlag(grid, IFFFlags.HideLabel);
        }
    }

    private void OnIFFShow(EntityUid uid, IFFConsoleComponent component, IFFShowIFFMessage args)
    {
        if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid == null)
        {
            return;
        }

        if (!args.Show)
        {
            AddAllSupportedIFFFlags(xform, component);
        }
        else
        {
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
        }
    }

    private void OnInitIFFConsole(EntityUid uid, IFFConsoleComponent component, MapInitEvent args)
    {
        if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid == null)
        {
            return;
        }

        if (component.HideOnInit)
        {
            AddAllSupportedIFFFlags(xform, component);
        }
    }

    private void OnIFFSetColor(EntityUid uid, IFFConsoleComponent component, IFFSetColorMessage args)
    {
        if (!component.AllowColorChange ||
            !TryComp(uid, out TransformComponent? xform) ||
            xform.GridUid is not { } gridUid)
        {
            return;
        }

        var parsed = Color.TryFromHex(args.ColorHex);
        if (!parsed.HasValue)
            return;

        var normalized = IFFComponent.NormalizeSignatureColor(parsed.Value);
        SetIFFColor(gridUid, normalized);
    }

    private void OnIFFSetDesignation(EntityUid uid, IFFConsoleComponent component, IFFSetDesignationMessage args)
    {
        if (!component.AllowDesignationChange ||
            !TryComp(uid, out TransformComponent? xform) ||
            xform.GridUid is not { } gridUid)
        {
            return;
        }

        var iff = EnsureComp<IFFComponent>(gridUid);
        if (iff.Designation == args.Designation)
            return;

        iff.Designation = args.Designation;
        Dirty(gridUid, iff);
        UpdateIFFInterfaces(gridUid, iff);
    }

    private void OnIFFConsoleAnchor(EntityUid uid, IFFConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        // If we anchor / re-anchor then make sure flags up to date.
        if (!args.Anchored ||
            !TryComp(uid, out TransformComponent? xform) ||
            !TryComp<IFFComponent>(xform.GridUid, out var iff))
        {
            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = IFFFlags.None,
                SignatureColor = IFFComponent.IFFColor,
                ColorEditable = component.AllowColorChange,
                Designation = IFFDesignation.Other,
                DesignationEditable = component.AllowDesignationChange,
            });
        }
        else
        {
            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = iff.Flags,
                SignatureColor = iff.Color,
                ColorEditable = component.AllowColorChange,
                Designation = iff.Designation,
                DesignationEditable = component.AllowDesignationChange,
            });
        }
    }

    protected override void UpdateIFFInterfaces(EntityUid gridUid, IFFComponent component)
    {
        base.UpdateIFFInterfaces(gridUid, component);

        var query = AllEntityQuery<IFFConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.GridUid != gridUid)
                continue;

            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = comp.AllowedFlags,
                Flags = component.Flags,
                SignatureColor = component.Color,
                ColorEditable = comp.AllowColorChange,
                Designation = component.Designation,
                DesignationEditable = comp.AllowDesignationChange,
            });
        }
    }

    // Made this method to avoid copy and pasting.
    /// <summary>
    /// Adds all IFF flags that are allowed by AllowedFlags to the grid.
    /// </summary>
    private void AddAllSupportedIFFFlags(TransformComponent xform, IFFConsoleComponent component)
    {
        if (xform.GridUid == null)
        {
            return;
        }

        if ((component.AllowedFlags & IFFFlags.HideLabel) != 0x0)
        {
            AddIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
        }
        if ((component.AllowedFlags & IFFFlags.Hide) != 0x0)
        {
            AddIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
        }
    }
}
