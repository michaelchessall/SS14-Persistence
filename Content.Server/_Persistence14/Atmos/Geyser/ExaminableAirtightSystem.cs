using Content.Shared._Persistence14.Atmos.Components;
using Content.Shared.Examine;


namespace Content.Server._Persistence14.Atmos;

public sealed partial class ExaminableAirtightSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
    }

    [SubscribeLocalEvent]
    public void OnExamine(Entity<StructureLeakVisualsComponent> ent, ref ExaminedEvent args)
    {
        if (!ProtoMan.Resolve(ent.Comp.Messages, out var proto))
            return;

        var index = ent.Comp.Leaking ? 1 : 0;
        if (index >= proto.Values.Count)
            return;

        var msg = Loc.GetString(proto.Values[index]);
        args.PushMarkup(msg, -99);
    }
}
