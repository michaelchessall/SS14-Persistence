using System.Linq;
using Content.Server._Persistence14.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Sound.Components;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Persistence14.DeviceLinking.Systems;

/// <summary>
/// Handles the "plushie sound activator" gadget. While the activator sits inside a plushie, the plushie
/// is given one device-link sink port per distinct sound it can make, and a signal on any of those ports
/// plays the matching sound. Pull the activator back out and the ports (and their links) go away.
/// </summary>
public sealed class PlushieSoundActivatorSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// Prefix for the sink port prototypes. Ports are named PlushieSound1, PlushieSound2, ...
    /// </summary>
    private const string PortPrefix = "PlushieSound";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlushieSoundActivatorComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<PlushieSoundActivatorComponent, EntGotRemovedFromContainerMessage>(OnRemoved);

        SubscribeLocalEvent<PlushieSoundLinkComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInserted(Entity<PlushieSoundActivatorComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var plushie = args.Container.Owner;

        // Already wired into something, or the target already has links set up by another activator.
        if (ent.Comp.LinkedPlushie != null || HasComp<PlushieSoundLinkComponent>(plushie))
            return;

        var sounds = GetPlushieSounds(plushie);
        if (sounds.Count == 0)
            return; // Not a sound-making toy; nothing to expose.

        var count = Math.Min(sounds.Count, ent.Comp.MaxSounds);
        var link = new PlushieSoundLinkComponent { Activator = ent.Owner };
        var ports = new List<ProtoId<SinkPortPrototype>>(count);

        for (var i = 0; i < count; i++)
        {
            var portId = $"{PortPrefix}{i + 1}";
            if (!_proto.HasIndex<SinkPortPrototype>(portId))
                break; // Ran out of defined port prototypes.

            link.PortSounds[portId] = sounds[i];
            ports.Add(portId);
        }

        if (ports.Count == 0)
            return;

        _deviceLink.EnsureSinkPorts(plushie, ports.ToArray());
        AddComp(plushie, link);
        ent.Comp.LinkedPlushie = plushie;
    }

    private void OnRemoved(Entity<PlushieSoundActivatorComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        ent.Comp.LinkedPlushie = null;
        Cleanup(args.Container.Owner);
    }

    /// <summary>
    /// Tears down the sink ports and links that were added to a plushie by an activator.
    /// </summary>
    private void Cleanup(EntityUid plushie)
    {
        if (!HasComp<PlushieSoundLinkComponent>(plushie))
            return;

        // Drop every link pointing at this plushie, then strip the sink entirely so no ports linger.
        if (HasComp<DeviceLinkSinkComponent>(plushie))
        {
            _deviceLink.RemoveAllFromSink(plushie);
            RemComp<DeviceLinkSinkComponent>(plushie);
        }

        RemComp<PlushieSoundLinkComponent>(plushie);
    }

    private void OnSignalReceived(Entity<PlushieSoundLinkComponent> ent, ref SignalReceivedEvent args)
    {
        if (!ent.Comp.PortSounds.TryGetValue(args.Port, out var sound))
            return;

        // Ignore the falling edge of toggle-style sources so we only play once per activation.
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);
        if (state == SignalState.Low)
            return;

        _audio.PlayPvs(sound, ent.Owner);
    }

    /// <summary>
    /// Collects the distinct sounds a plushie can play, expanding sound collections into their
    /// individual files so that each sound can be linked to a separate port.
    /// </summary>
    private List<SoundSpecifier> GetPlushieSounds(EntityUid plushie)
    {
        var result = new List<SoundSpecifier>();
        var seen = new HashSet<string>();

        void Collect(SoundSpecifier? sound)
        {
            if (sound == null)
                return;

            foreach (var spec in ExpandSound(sound))
            {
                if (seen.Add(SoundKey(spec)))
                    result.Add(spec);
            }
        }

        // The common emit-sound components (use/activate/collide/land) all share a base type.
        foreach (var emit in AllComps<BaseEmitSoundComponent>(plushie))
        {
            Collect(emit.Sound);
        }

        // EmitSoundOnTrigger keeps its own Sound field and does not derive from the base above.
        if (TryComp<EmitSoundOnTriggerComponent>(plushie, out var trigger))
            Collect(trigger.Sound);

        return result;
    }

    /// <summary>
    /// Turns a sound specifier into the individual sounds it represents. A collection becomes one
    /// specifier per file so every squeak can be addressed independently.
    /// </summary>
    private IEnumerable<SoundSpecifier> ExpandSound(SoundSpecifier sound)
    {
        switch (sound)
        {
            case SoundPathSpecifier path:
                yield return path;
                break;
            case SoundCollectionSpecifier collection when collection.Collection != null
                && _proto.TryIndex<SoundCollectionPrototype>(collection.Collection, out var proto):
                foreach (var file in proto.PickFiles)
                    yield return new SoundPathSpecifier(file, collection.Params);
                break;
            default:
                yield return sound;
                break;
        }
    }

    private static string SoundKey(SoundSpecifier sound)
    {
        return sound switch
        {
            SoundPathSpecifier path => "path:" + path.Path,
            SoundCollectionSpecifier collection => "collection:" + collection.Collection,
            _ => sound.ToString() ?? sound.GetHashCode().ToString(),
        };
    }
}
