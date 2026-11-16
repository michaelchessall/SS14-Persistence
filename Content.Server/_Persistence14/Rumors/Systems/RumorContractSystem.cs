using System.Linq;
using Content.Server.Popups;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared._Persistence14.Rumors.Systems;
using Content.Shared.Interaction.Events;
using Robust.Server.Audio;

namespace Content.Server._Persistence14.Rumors.Systems;

public sealed partial class RumorContractSystem : EntitySystem
{
    [Dependency] private RumorsSystem _rumors = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<RumorContractComponent> contract, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        UseContract(contract, args.User);
        args.Handled = true;
    }

    private void UseContract(Entity<RumorContractComponent> contract, EntityUid user)
    {
        var getter = _rumors.GetRumorComponent(user);
        if (getter is null || getter.Value.Comp.Rumors.Count >= getter.Value.Comp.MaxRumorCount ||
            !_rumors.AssignRumor(getter.Value.Comp, user, contract.Comp.Faction))
        {
            _popup.PopupEntity($"Can't do that right now!", user, Shared.Popups.PopupType.Small);
            return;
        }

        _audio.PlayLocal(contract.Comp.UseSound, contract.Owner, user);
        _popup.PopupEntity($"Contract accepted!", user, Shared.Popups.PopupType.Small);
        QueueDel(contract);
    }
}