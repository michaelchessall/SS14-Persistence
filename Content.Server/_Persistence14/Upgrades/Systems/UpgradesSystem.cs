using Content.Server.Examine;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Kitchen.EntitySystems;
using Content.Server.Lathe;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Generation.Teg;
using Content.Server.Salvage.Magnet;
using Content.Server.Shuttles.Systems;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Tools;
using Content.Shared._Persistence14.Upgrades.Components;
using Content.Shared._Persistence14.Upgrades.Prototypes;
using Content.Shared._Persistence14.Upgrades.Systems;
using Content.Shared.Armor;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Kitchen.Components;
using Content.Shared.Lathe;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Prayer;
using Content.Shared.Radio.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using NetCord;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.ComponentModel;
using System.Linq;

namespace Content.Server._Persistence14.Upgrades.Systems;


public sealed partial class UpgradesSystem : SharedUpgradesSystem
{
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private EntityQuery<UpgradeModuleComponent> _upgradeModuleQuery = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<RadarConsoleComponent> _radarQuery = default!;
    [Dependency] private EntityQuery<ApcComponent> _apcQuery = default!;
    [Dependency] private EntityQuery<EntityStorageComponent> _entityStorageQuery = default!;
    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;
    [Dependency] private EntityQuery<TelecomServerComponent> _telecomServerQuery = default!;
    [Dependency] private EntityQuery<LatheComponent> _latheQuery = default!;
    [Dependency] private EntityQuery<MicrowaveComponent> _microwaveQuery = default!;
    [Dependency] private EntityQuery<GasTankComponent> _gasTankQuery = default!;
    [Dependency] private EntityQuery<ReagentGrinderComponent> _reagentGrinderQuery = default!;
    [Dependency] private EntityQuery<GasCanisterComponent> _gasCanisterQuery = default!;
    [Dependency] private EntityQuery<TegGeneratorComponent> _tegGeneratorQuery = default!;
    [Dependency] private EntityQuery<RadiationCollectorComponent> _radiationCollectorQuery = default!;
    [Dependency] private EntityQuery<ToolComponent> _toolQuery = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private RadarConsoleSystem _radar = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private ReagentGrinderSystem _reagentGrinder = default!;
    [Dependency] private TegSystem _teg = default!;
    [Dependency] private ToolSystem _tool = default!;
    [Dependency] private RadiationCollectorSystem _radiationCollector = default!;
    [Dependency] private ExamineSystem _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UpgradeableComponent, GetVerbsEvent<ActivationVerb>>(AddUpgradeVerb);
        SubscribeLocalEvent<UpgradeableComponent, GetVerbsEvent<ExamineVerb>>(AddExamineVerb);
    }

    private void AddExamineVerb(Entity<UpgradeableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanAccess)
            return;
        if (!TryComp(args.User, out ActorComponent? actor))
            return;
        FormattedMessage msg = new();
        msg.TryAddMarkup(GetUpgradeableExamineText(ent), out _);
        _examine.AddDetailedExamineVerb(args, ent.Comp, msg, "Upgrades", hoverMessage: "Examine the upgrades.");
    }

    private string GetUpgradeableExamineText(Entity<UpgradeableComponent> ent)
    {
        string msg = "";
        foreach(var kv in ent.Comp.Upgrades)
        {
            var upgrade = kv.Key;
            var appliedTimes = kv.Value;
            var upgradeProto = _protoMan.Index(upgrade);
            var moduleTypeProto = _protoMan.Index(upgradeProto.TargetModule);
            msg += $"\n\n[bold]{upgradeProto.Name} ({moduleTypeProto.Name})[/bold]:\n[color=yellow]{upgradeProto.Description}[/color] ({appliedTimes}/{upgradeProto.MaxApplications})";
        }
        if (!ent.Comp.Chosen && ent.Comp.ChooseOneUpgrades.Count > 0)
        {
            msg += $"\nChoose one:";
        }
        foreach (var kv in ent.Comp.ChooseOneUpgrades)
        {
            if(ent.Comp.Chosen)
            {
                if (kv.Value == 0) continue;
            }
            var upgrade = kv.Key;
            var appliedTimes = kv.Value;
            var upgradeProto = _protoMan.Index(upgrade);
            var moduleTypeProto = _protoMan.Index(upgradeProto.TargetModule);
            msg += $"\n\n[bold]{upgradeProto.Name} ({moduleTypeProto.Name})[/bold]:\n[color=yellow]{upgradeProto.Description}[/color] ({appliedTimes}/{upgradeProto.MaxApplications})";
                
        }

        return msg;
    }

    private void AddUpgradeVerb(EntityUid uid, UpgradeableComponent comp, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess)
            return;
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
        foreach (var kv in comp.ChooseOneUpgrades)
        {
            if (comp.Chosen && kv.Value == 0) continue;
            var upgrade = kv.Key;
            var appliedTimes = kv.Value;
            var upgradeProto = _protoMan.Index(upgrade);
            if (upgradeProto == null) continue;
            if (upgradeProto.TargetModule == upgradeModuleComp.TargetModule && appliedTimes < upgradeProto.MaxApplications)
            {
                ApplyUpgrade(uid, comp, value, upgradeModuleComp, user);
                comp.Chosen = true;
                return true;
            }
        }
        return false;
    }

    private void ApplyUpgrade(EntityUid uid, UpgradeableComponent comp, EntityUid moduleUid, UpgradeModuleComponent upgradeModuleComp, EntityUid user)
    {
        Dictionary<ProtoId<UpgradePrototype>, int> full_upgrades = comp.Upgrades.Concat(comp.ChooseOneUpgrades).ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var kv in full_upgrades)
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
                if(upgradeProto.UpgradeType == UpgradeType.EnttiyStorageCapacity)
                {
                    ApplyEntityStorageCapacityUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.BatteryCapacity)
                {
                    ApplyBatteryCapacityUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.TelecomServerRange)
                {
                    ApplyTelecomServerRangeUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.LatheSpeed)
                {
                    ApplyLatheSpeedUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.MicrowaveCapacity)
                {
                    ApplyMicrowaveCapacityUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.MicrowaveSpeed)
                {
                    ApplyMicrowaveSpeedUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.AirTankCapacity)
                {
                    ApplyAirTankCapacityUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.ReagentGrinderSpeed)
                {
                    ApplyReagentGrinderSpeedUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.GasCanisterCapacity)
                {
                    ApplyGasCanisterCapacityUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.TegGeneratorPower)
                {
                    ApplyTegGeneratorPowerUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.ToolSpeed)
                {
                    ApplyToolSpeedUpgrade(uid, upgradeProto);
                }
                if (upgradeProto.UpgradeType == UpgradeType.RadiationCollectorPower)
                {
                    ApplyRadiationCollectorPowerUpgrade(uid, upgradeProto);
                }
                if(upgradeProto.UpgradeType == UpgradeType.Armor)
                {
                    ApplyArmorUpgrade(uid, upgradeProto);
                }
                if(comp.Upgrades.ContainsKey(upgrade))
                {
                    comp.Upgrades[upgrade] = appliedTimes + 1;
                }
                else if(comp.ChooseOneUpgrades.ContainsKey(upgrade))
                {
                    comp.ChooseOneUpgrades[upgrade] = appliedTimes + 1;
                }
                QueueDel(moduleUid);
                _audioSystem.PlayPredicted(new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg"), uid, null);
                return;
            }
        }
    }

    private void ApplyArmorUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if(upgradeProto.ArmorModifiers == null) return;
        EnsureComp<ArmorComponent>(uid, out var armorComp);
        if(armorComp.Modifiers == null)
        {
            armorComp.Modifiers = new DamageModifierSet();
        }
        foreach (var damageCo in upgradeProto.ArmorModifiers.Coefficients)
        {
            if(armorComp.Modifiers.Coefficients.ContainsKey(damageCo.Key))
            {
                armorComp.Modifiers.Coefficients[damageCo.Key] -= damageCo.Value;
            }
            else
            {
                armorComp.Modifiers.Coefficients[damageCo.Key] = 1 - damageCo.Value;
            }
        }
        foreach (var damageFlat in upgradeProto.ArmorModifiers.FlatReductions)
        {
            if (armorComp.Modifiers.FlatReductions.ContainsKey(damageFlat.Key))
            {
                armorComp.Modifiers.FlatReductions[damageFlat.Key] += damageFlat.Value;
            }
            else
            {
                armorComp.Modifiers.FlatReductions[damageFlat.Key] = damageFlat.Value;
            }
        }
        Dirty(uid, armorComp);
    }

    private void ApplyRadiationCollectorPowerUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if(!_radiationCollectorQuery.TryComp(uid, out var radiationCollectorComp)) return;
        _radiationCollector.SetChargeModifier(uid, radiationCollectorComp.ChargeModifier + upgradeProto.UpgradeValues[0], radiationCollectorComp);
    }

    private void ApplyToolSpeedUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_toolQuery.TryComp(uid, out var toolComp)) return;
        _tool.SetToolSpeedModifier(uid, toolComp, toolComp.SpeedModifier + upgradeProto.UpgradeValues[0]);
    }

    private void ApplyTegGeneratorPowerUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_tegGeneratorQuery.TryComp(uid, out var tegGeneratorComp)) return;
        _teg.SetPowerFactor(uid, tegGeneratorComp, tegGeneratorComp.PowerFactor + upgradeProto.UpgradeValues[0]);
    }

    private void ApplyGasCanisterCapacityUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_gasCanisterQuery.TryComp(uid, out var gasCanisterComp)) return;
        gasCanisterComp.Air.Volume += upgradeProto.UpgradeValues[0];
    }

    private void ApplyReagentGrinderSpeedUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_reagentGrinderQuery.TryComp(uid, out var reagentGrinderComp)) return;
        _reagentGrinder.SetSpeedMultiplier((uid, reagentGrinderComp), reagentGrinderComp.WorkTimeMultiplier - upgradeProto.UpgradeValues[0]);
    }

    private void ApplyAirTankCapacityUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_gasTankQuery.TryComp(uid, out var gasTankComp)) return;
        gasTankComp.Air.Volume += upgradeProto.UpgradeValues[0];
    }

    private void ApplyMicrowaveSpeedUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if(!_microwaveQuery.TryComp(uid, out var microwaveComp)) return;
        microwaveComp.CookTimeMultiplier -= upgradeProto.UpgradeValues[0];
    }

    private void ApplyMicrowaveCapacityUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_microwaveQuery.TryComp(uid, out var microwaveComp)) return;
        microwaveComp.Capacity += (int)upgradeProto.UpgradeValues[0];
    }

    private void ApplyLatheSpeedUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if(!_latheQuery.TryComp(uid, out var latheComp)) return;
        latheComp.TimeMultiplier -= upgradeProto.UpgradeValues[0];
    }

    private void ApplyTelecomServerRangeUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if(!_telecomServerQuery.TryComp(uid, out var telecomServerComp)) return;
        telecomServerComp.MaxRange += upgradeProto.UpgradeValues[0];
    }

    private void ApplyBatteryCapacityUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if(!_batteryQuery.TryComp(uid, out var batteryComp)) return;
        _battery.SetMaxCharge((uid, batteryComp), batteryComp.MaxCharge + upgradeProto.UpgradeValues[0]);
    }

    private void ApplyEntityStorageCapacityUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (!_entityStorageQuery.TryComp(uid, out var entityStorageComp)) return;
        entityStorageComp.Capacity += (int)upgradeProto.UpgradeValues[0];
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
    }

    private void ApplyStorageUpgrade(EntityUid uid, UpgradePrototype upgradeProto)
    {
        if (_storageQuery.TryComp(uid, out var storageComp))
        {
            var x = upgradeProto.UpgradeValues[0];
            var y = upgradeProto.UpgradeValues[1];
            var box = storageComp.Grid[0];
            storageComp.Grid[0] = new Box2i(box.Left, box.Bottom, box.Right + (int)x, box.Top + (int)y);
            Dirty(uid, storageComp);
        }
    }
}
