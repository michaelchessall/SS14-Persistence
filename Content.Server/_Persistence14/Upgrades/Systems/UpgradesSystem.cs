using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Salvage.Magnet;
using Content.Server.Shuttles.Systems;
using Content.Shared._Persistence14.Upgrades.Components;
using Content.Shared._Persistence14.Upgrades.Prototypes;
using Content.Shared._Persistence14.Upgrades.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Prayer;
using Content.Shared.Shuttles.Components;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using NetCord;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.ComponentModel;

namespace Content.Server._Persistence14.Upgrades.Systems;


public sealed partial class UpgradesSystem : SharedUpgradesSystem
{
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private EntityQuery<UpgradeModuleComponent> _upgradeModuleQuery = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<RadarConsoleComponent> _radarQuery = default!;
    [Dependency] private EntityQuery<ApcComponent> _apcQuery = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private RadarConsoleSystem _radar = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UpgradeableComponent, GetVerbsEvent<ActivationVerb>>(AddUpgradeVerb);
        SubscribeLocalEvent<UpgradeableComponent, ExaminedEvent>(AddUpgradeableExamined);
    }

    private void AddUpgradeableExamined(Entity<UpgradeableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        string msg = "\nUpgrades:";
        foreach(var kv in ent.Comp.Upgrades)
        {
            var upgrade = kv.Key;
            var appliedTimes = kv.Value;
            var upgradeProto = _protoMan.Index(upgrade);
            var moduleTypeProto = _protoMan.Index(upgradeProto.TargetModule);
            msg += $"\n\n{upgradeProto.Name} ({moduleTypeProto.Name}):\n{upgradeProto.Description} ({appliedTimes}/{upgradeProto.MaxApplications})\n";
        }

        args.PushText(msg);
    }

    private void AddUpgradeVerb(EntityUid uid, UpgradeableComponent comp, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess)
            return;
        // if it doesn't have an actor and we can't reach it then don't add the verb
        if (!TryComp(args.User, out ActorComponent? actor))
            return;
        var upgradeVerb = new ActivationVerb
        {
            Text = Loc.GetString(comp.Verb),
            Icon = comp.VerbImage,
            Act = () =>
            {
                var item = _hands.GetActiveItem(args.User);
                if (item == null)
                {
                    _popupSystem.PopupEntity("You must be holding a valid upgrade module.", uid, actor.PlayerSession, PopupType.Large);
                    return;
                }
                if(TryApplyUpgade(uid, comp, item.Value, args.User))
                {
                    _popupSystem.PopupEntity("Upgrade applied successfully.", uid, actor.PlayerSession, PopupType.Large);
                }
                else
                {
                    _popupSystem.PopupEntity("Upgrade failed. Invalid module or incompatible upgrade.", uid, actor.PlayerSession, PopupType.Large);
                }
            },
            Impact = LogImpact.Low,

        };
        upgradeVerb.Impact = LogImpact.Low;
        args.Verbs.Add(upgradeVerb);
    }

    private bool TryApplyUpgade(EntityUid uid, UpgradeableComponent comp, EntityUid value, EntityUid user)
    {
        if (!_upgradeModuleQuery.TryComp(value, out var upgradeModuleComp)) return false;
        foreach(var kv in comp.Upgrades)
        {
            var upgrade = kv.Key;
            var appliedTimes = kv.Value;
            var upgradeProto = _protoMan.Index(upgrade);
            if (upgradeProto == null) continue;
            if(upgradeProto.TargetModule == upgradeModuleComp.TargetModule && appliedTimes < upgradeProto.MaxApplications)
            {
                ApplyUpgrade(uid, comp, value, upgradeModuleComp, user);
                return true;
            }
        }
        return false;
    }

    private void ApplyUpgrade(EntityUid uid, UpgradeableComponent comp, EntityUid moduleUid, UpgradeModuleComponent upgradeModuleComp, EntityUid user)
    {
        foreach (var kv in comp.Upgrades)
        {
            var upgrade = kv.Key;
            var appliedTimes = kv.Value;
            var upgradeProto = _protoMan.Index(upgrade);
            if (upgradeProto == null) continue;
            if (upgradeProto.TargetModule == upgradeModuleComp.TargetModule && appliedTimes < upgradeProto.MaxApplications)
            {
                if(upgradeProto.UpgradeType == UpgradeType.Storage)
                {
                    ApplyStorageUpgrade(uid, upgradeProto);
                }
                if(upgradeProto.UpgradeType == UpgradeType.RadarRange)
                {
                    ApplyRadarRangeUpgrade(uid, upgradeProto);
                }
                if(upgradeProto.UpgradeType == UpgradeType.ApcMaxLoad)
                {
                    ApplyApcMaxLoadUpgrade(uid, upgradeProto);
                }
                comp.Upgrades[upgrade] = appliedTimes + 1;
                QueueDel(moduleUid);
                _audioSystem.PlayPredicted(new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg"), uid, null);
                return;
            }
        }
    }

    private void ApplyApcMaxLoadUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_apcQuery.TryComp(uid, out var apcComponent)) return;
        apcComponent.MaxLoad += upgradeProto.UpgradeValues[0];

    }

    private void ApplyRadarRangeUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_radarQuery.TryComp(uid, out var radarComp)) return;
        _radar.SetRange(uid, radarComp.MaxRange + upgradeProto.UpgradeValues[0], radarComp);
        Dirty(uid, radarComp);
    }

    private void ApplyStorageUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (_storageQuery.TryComp(uid, out var storageComp))
        {
            var x = upgradeProto.UpgradeValues[0];
            var y = upgradeProto.UpgradeValues[1];
            var box = storageComp.Grid[0];
            storageComp.Grid[0] = new Box2i(box.Left, box.Bottom, box.Right + x, box.Top + y);
            Dirty(uid, storageComp);
        }
    }
}
