using System.Linq;
using Content.Shared.Station.Components;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem
{
    /*
     * Handles the label visibility on radar controls. This can be hiding the label or applying other effects.
     */

    protected virtual void UpdateIFFInterfaces(EntityUid gridUid, IFFComponent component) { }

    public Color GetIFFColor(EntityUid gridUid, bool self = false, IFFComponent? component = null)
    {
        if (self)
        {
            return IFFComponent.SelfColor;
        }

        if (!Resolve(gridUid, ref component, false))
        {
            return IFFComponent.IFFColor;
        }

        return component.Color;
    }

    public string? GetIFFLabel(EntityUid gridUid, bool self = false, IFFComponent? component = null)
    {
        var entName = MetaData(gridUid).EntityName;

        var baseName = string.IsNullOrEmpty(entName) ? Loc.GetString("shuttle-console-unknown") : entName;

        Resolve(gridUid, ref component, false);

        if (!self && component != null && (component.Flags & (IFFFlags.HideLabel | IFFFlags.Hide)) != 0x0)
        {
            return null;
        }

        // Default to showing faction tags when there is no IFF component yet.
        // This keeps claimed/owned grids readable on radar without extra setup.
        var showFactionTag = component?.ShowFactionTag ?? true;
        if (showFactionTag)
        {
            var station = Station.GetOwningStation(gridUid);
            if (station != null && TryComp<StationDataComponent>(station, out var stationData))
            {
            // Prefix format mirrors ID cards for quick visual consistency.
                var tag = stationData.GetResolvedFactionTag(MetaData(station.Value).EntityName);
                if (!string.IsNullOrEmpty(tag))
                    return $"[{tag}] {baseName}";
            }
        }

        return baseName;
    }

    /// <summary>
    /// Sets the color for this grid to appear as on radar.
    /// </summary>
    [PublicAPI]
    public void SetIFFColor(EntityUid gridUid, Color color, IFFComponent? component = null)
    {
        component ??= EnsureComp<IFFComponent>(gridUid);
        color = IFFComponent.NormalizeSignatureColor(color);

        if (component.Color.Equals(color))
            return;

        component.Color = color;
        Dirty(gridUid, component);
        UpdateIFFInterfaces(gridUid, component);
    }

    [PublicAPI]
    public void AddIFFFlag(EntityUid gridUid, IFFFlags flags, IFFComponent? component = null)
    {
        component ??= EnsureComp<IFFComponent>(gridUid);

        if ((component.Flags & flags) == flags)
            return;

        component.Flags |= flags;
        Dirty(gridUid, component);
        UpdateIFFInterfaces(gridUid, component);
    }

    [PublicAPI]
    public void RemoveIFFFlag(EntityUid gridUid, IFFFlags flags, IFFComponent? component = null)
    {
        if (!Resolve(gridUid, ref component, false))
            return;

        if ((component.Flags & flags) == 0x0)
            return;

        component.Flags &= ~flags;
        Dirty(gridUid, component);
        UpdateIFFInterfaces(gridUid, component);
    }

    public bool MatchesSortTags(IFFComponent? component, IFFSortMode sortMode)
    {
        if (sortMode == IFFSortMode.None)
            return false;

        return component?.Designation switch
        {
            IFFDesignation.Station => sortMode.HasFlag(IFFSortMode.Station),
            IFFDesignation.Ship => sortMode.HasFlag(IFFSortMode.Ship),
            _ => sortMode.HasFlag(IFFSortMode.Other),
        };
    }
}
