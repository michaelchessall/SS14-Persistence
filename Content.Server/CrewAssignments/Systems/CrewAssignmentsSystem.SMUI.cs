using Content.Server._Persistence14.Codex.Systems;
using Content.Server._Persistence14.Resonance.Systems;
using Content.Server.Chat.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.Hands.Systems;
using Content.Shared._Persistence14.Resonance.Prototypes;
using Content.Shared._Persistence14.Upgrades.Components;
using Content.Shared.Access;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.CrewAccesses.Components;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewAssignments.Events;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.MessageBoard.Components;
using Content.Shared.Radio;
using Content.Shared.Station.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using System.Linq;

namespace Content.Server.CrewAssignments.Systems;

public sealed partial class CrewAssignmentSystem
{

    [Dependency] private CrewMetaRecordsSystem _crewMeta = default!;
    [Dependency] private ResonanceSystem _resonance = default!;
    [Dependency] private CodexSystem _codex = default!;
    [Dependency] private HandsSystem _hands = default!;
    private void InitializeConsole()
    {
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeSalesTax>(OnChangeSalesTax);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeExportTax>(OnChangeExportTax);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeImportTax>(OnChangeImportTax);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationRemoveOwner>(OnRemoveOwner);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationAddOwner>(OnAddOwner);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeName>(OnChangeName);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeFactionTag>(OnChangeFactionTag);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationAddAccess>(OnAddAccess);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationRemoveAccess>(OnDeleteAccess);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationCreateAssignment>(OnCreateAssignment);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationToggleAssignmentAccess>(OnToggleAccess);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationToggleChannelAccess>(OnToggleChannelAccess);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationEnableChannel>(OnEnableChannel);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationDisableChannel>(OnDisableChannel);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationCreateChannel>(OnCreateChannel);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationEditChannel>(OnEditChannel);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationDeleteChannel>(OnDeleteChannel);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationToggleClaim>(OnToggleClaim);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationToggleGenRec>(OnToggleGenRec);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationToggleAssign>(OnToggleAssign);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeAssignmentCLevel>(OnChangeCLevel);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeAssignmentWage>(OnChangeWage);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeAssignmentName>(OnChangeAName);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationChangeAssignmentSpendingLimit>(OnChangeSpendingLimit);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationDeleteAssignment>(OnDeleteAssignment);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationDefaultAccess>(OnDefaultAccess);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationJobNetOn>(OnJobNetOn);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationJobNetOff>(OnJobNetOff);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationResonancePurchase>(OnResonancePurchase);
        SubscribeLocalEvent<StationModificationConsoleComponent, StationModificationApplyModule>(OnApplyModule);
        SubscribeLocalEvent<StationModificationConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
        SubscribeLocalEvent<StationModificationConsoleComponent, ComponentInit>(OnInit);
    }

    private void OnApplyModule(Entity<StationModificationConsoleComponent> ent, ref StationModificationApplyModule args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(ent);
        if (station == null) return;

        if (!Validate(ent, ent.Comp, player, out var stationData)) return;
        if (!TryComp<StationDataComponent>(station, out var data) || data == null)
            return;
        _protoMan.Resolve(data.Level, out var currentLevel);
        if (currentLevel == null || currentLevel.Next == null) return;
        _protoMan.Resolve(currentLevel.Next, out var nextLevel);
        if (nextLevel == null) return;
        if (!nextLevel.RequiredModules.ContainsKey(args.Prototype)) return;
        var module = _hands.GetActiveItem(player);
        if (module == null || !TryComp<UpgradeModuleComponent>(module, out var upgradeComp) || upgradeComp == null || upgradeComp.TargetModule != args.Prototype)
        {
            _codex.NotifyPlayer(player, $"You must be holding an upgrade module of the correct type in your active hand.");
            return;
        }
        if (data.UpgradeProgress.ContainsKey(args.Prototype))
        {
            if (data.UpgradeProgress[args.Prototype] >= nextLevel.RequiredModules[args.Prototype])
            {
                return;
            }
            data.UpgradeProgress[args.Prototype]++;
        }
        else data.UpgradeProgress.Add(args.Prototype, 1);
        _codex.NotifyPlayer(player, "Upgade Module Applied.", upgradeComp.ApplySound);
        QueueDel(module);
        UpdateOrders(station.Value);
        if (data.UpgradeProgress[args.Prototype] < nextLevel.RequiredModules[args.Prototype]) return;
        foreach (var kv in nextLevel.RequiredModules)
        {
            if (!data.UpgradeProgress.ContainsKey(kv.Key)) return;
            if (data.UpgradeProgress[kv.Key] < kv.Value) return;
        }
        data.UpgradeProgress = new();
        data.Level = currentLevel.Next.Value;
        UpdateOrders(station.Value);
    }

    private void OnResonancePurchase(Entity<StationModificationConsoleComponent> ent, ref StationModificationResonancePurchase args)
    {
        _resonance.OnResonancePurchase(ent, args);
        UpdateAllUserInterfaces();
    }

    private void OnInit(EntityUid uid, StationModificationConsoleComponent orderConsole, ComponentInit args)
    {
        var station = _station.GetOwningStation(uid);
        UpdateOrderState(uid, station);
    }

    #region Interface

    private bool Validate(EntityUid uid, StationModificationConsoleComponent component, EntityUid player, out StationDataComponent? stationData)
    {
        var station = _station.GetOwningStation(uid);

        if (station == null)
        {
            stationData = null;
            return false;
        }

        // No station to deduct from.
        if (!TryComp(station, out StationDataComponent? sD))
        {
            ConsolePopup(player, "Station not found!");
            stationData = null;
            return false;
        }
        stationData = sD;
        if (stationData.Owners.Count > 0 && !stationData.IsOwner(Name(player)))
        {
            ConsolePopup(player, "Access denied.");
            return false;
        }

        return true;
    }

    private void OnRemoveOwner(EntityUid uid, StationModificationConsoleComponent component, StationModificationRemoveOwner args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        // No station to deduct from.
        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Owner == Name(player))
        {
            ConsolePopup(args.Actor, "You cannot remove yourself.");
            return;
        }
        stationData!.RemoveOwner(args.Owner);
        Dirty((EntityUid)station, stationData);
        UpdateOrders(station.Value);

    }

    private void OnChangeSpendingLimit(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeAssignmentSpendingLimit args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Limit < 0)
        {
            ConsolePopup(player, "Cannot be below zero!");
            return;
        }
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        crewAssignment.SpendingLimit = args.Limit;
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);

    }
    private void OnChangeAName(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeAssignmentName args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Owner == null || args.Owner == "") return;
        if (args.Owner.Length > 24)
        {
            ConsolePopup(player, "Exceeded Maximum Length of 24 Characters!");
            return;
        }
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        foreach (var pair in crewAssignments.CrewAssignments)
        {
            if (pair.Value.Name == args.Owner)
            {
                ConsolePopup(player, "An assignment with that name already exists!");
                return;
            }
        }
        crewAssignment.Name = args.Owner;
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);
        RaiseLocalEvent(new StationRenameAssignment(station.Value));

    }
    private void OnCreateAssignment(EntityUid uid, StationModificationConsoleComponent component, StationModificationCreateAssignment args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Owner == null || args.Owner == "") return;
        if (args.Owner.Length > 24)
        {
            ConsolePopup(player, "Exceeded Maximum Length of 24 Characters!");
            return;
        }
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        foreach (var pair in crewAssignments.CrewAssignments)
        {
            if (pair.Value.Name == args.Owner)
            {
                ConsolePopup(player, "An assignment with that name already exists!");
                return;
            }
        }
        crewAssignments.CreateAssignment(args.Owner);
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);
        RaiseLocalEvent(new StationCreateAssignment(station.Value));
    }

    private void OnChangeSalesTax(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeSalesTax args)
    {
        if (args.Level < 0 || args.Level > 100) return;
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Level < 0) return;
        stationData!.SalesTax = args.Level;
        UpdateOrders(station.Value);

    }
    private void OnChangeExportTax(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeExportTax args)
    {
        if (args.Level < 0 || args.Level > 100) return;
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Level < 0) return;
        stationData!.ExportTax = args.Level;
        UpdateOrders(station.Value);

    }
    private void OnChangeImportTax(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeImportTax args)
    {
        if (args.Level < 0 || args.Level > 200) return;
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Level < 0) return;
        stationData!.ImportTax = args.Level;
        UpdateOrders(station.Value);

    }
    private void OnChangeCLevel(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeAssignmentCLevel args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        if (args.Level < 0) return;
        crewAssignment.Clevel = args.Level;
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);

    }

    private void OnJobNetOff(EntityUid uid, StationModificationConsoleComponent component, StationModificationJobNetOff args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData) || stationData == null) return;
        stationData.JobNetEnabled = false;

        _station2.ClockOutEmployees(station.Value);
        UpdateOrders(station.Value);
    }
    private void OnJobNetOn(EntityUid uid, StationModificationConsoleComponent component, StationModificationJobNetOn args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData) || stationData == null) return;
        stationData.JobNetEnabled = true;
        UpdateOrders(station.Value);
    }
    private void OnDefaultAccess(EntityUid uid, StationModificationConsoleComponent component, StationModificationDefaultAccess args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAccessesComponent? crewAccesses))
        {
            ConsolePopup(player, "No CrewAccesses Component!");
            stationData = null;
            return;
        }
        foreach (var accessLevel in _protoMan.EnumeratePrototypes<AccessLevelPrototype>())
        {
            if (!accessLevel.CanAddToIdCard || crewAccesses.CrewAccesses.ContainsKey(accessLevel.ID))
            {
                continue;
            }
            crewAccesses.CreateAccess(accessLevel.ID);
        }
        Dirty((EntityUid)station, crewAccesses);
        UpdateOrders(station.Value);
    }

    private void OnDeleteAssignment(EntityUid uid, StationModificationConsoleComponent component, StationModificationDeleteAssignment args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        crewAssignments.CrewAssignments.Remove(args.AccessID);
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);
        RaiseLocalEvent(new StationDeleteAssignment(station.Value));

    }
    private void OnChangeWage(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeAssignmentWage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        if (args.Wage < 0) return;
        crewAssignment.Wage = args.Wage;
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);

    }

    private void OnEnableChannel(EntityUid uid, StationModificationConsoleComponent component, StationModificationEnableChannel args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp<StationDataComponent>(station, out var sD))
        {
            ConsolePopup(player, "No Station Data Component!");
            return;
        }
        if (!sD.RadioData.TryGetValue(args.ChannelID, out var channelData))
        {
            ConsolePopup(player, "Invalid Channel!");
            return;
        }
        channelData.Enabled = true;
        Dirty((EntityUid)station, sD);
        UpdateOrders(station.Value);
    }

    private void OnDisableChannel(EntityUid uid, StationModificationConsoleComponent component, StationModificationDisableChannel args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp<StationDataComponent>(station, out var sD))
        {
            ConsolePopup(player, "No Station Data Component!");
            return;
        }
        if (!sD.RadioData.TryGetValue(args.ChannelID, out var channelData))
        {
            ConsolePopup(player, "Invalid Channel!");
            return;
        }
        channelData.Enabled = false;
        Dirty((EntityUid)station, sD);
        UpdateOrders(station.Value);
    }
    private void OnToggleChannelAccess(EntityUid uid, StationModificationConsoleComponent component, StationModificationToggleChannelAccess args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp<StationDataComponent>(station, out var sD))
        {
            ConsolePopup(player, "No Station Data Component!");
            return;
        }
        if (!sD.RadioData.TryGetValue(args.ChannelID, out var channelData))
        {
            ConsolePopup(player, "Invalid Channel!");
            return;
        }
        if (args.ToggleState)
        {
            if (channelData.Access.Contains(args.Access))
            {
                return;
            }
            else
            {
                channelData.Access.Add(args.Access);
            }
        }
        else
        {
            if (channelData.Access.Contains(args.Access))
            {
                channelData.Access.Remove(args.Access);
            }
            else
            {
                return;
            }
        }
        Dirty(station.Value, sD);
        UpdateOrders(station.Value);
    }

    private static readonly ProtoId<RadioChannelPrototype>[] CustomChannelPool =
    {
        "FactionCustom1",
        "FactionCustom2",
        "FactionCustom3",
        "FactionCustom4",
        "FactionCustom5",
        "FactionCustom6",
        "FactionCustom7",
        "FactionCustom8",
        "FactionCustom9",
        "FactionCustom10",
        "FactionCustom11",
        "FactionCustom12",
    };

    private static string ClampBrightCustomColor(int red, int green, int blue)
    {
        // Persistence 14: Clamp only HSV value into the brightest third so saved custom radio colors stay readable without shifting their hue or saturation.
        var color = new Color(
            (byte) Math.Clamp(red, 0, 255),
            (byte) Math.Clamp(green, 0, 255),
            (byte) Math.Clamp(blue, 0, 255));

        var hsv = Color.ToHsv(color);
        hsv.Z = Math.Clamp(hsv.Z, 2f / 3f, 1f);
        return Color.FromHsv(hsv).ToHex();
        // End Persistence 14
    }

    private void OnCreateChannel(EntityUid uid, StationModificationConsoleComponent component, StationModificationCreateChannel args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null)
            return;

        if (!Validate(uid, component, player, out _))
            return;

        if (!TryComp<StationDataComponent>(station, out var stationData))
        {
            ConsolePopup(player, "No Station Data Component!");
            return;
        }

        var name = args.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ConsolePopup(player, "Channel name is required.");
            return;
        }

        if (name.Length > 24)
            name = name[..24];

        if (stationData.RadioData.Values.Any(data =>
                !string.IsNullOrWhiteSpace(data.CustomName) &&
                string.Equals(data.CustomName, name, StringComparison.OrdinalIgnoreCase)))
        {
            ConsolePopup(player, "A channel with that name already exists.");
            return;
        }

        var hotkeyText = args.Hotkey?.Trim() ?? string.Empty;
        if (hotkeyText.Length != 1)
        {
            ConsolePopup(player, "Hotkey must be exactly one character.");
            return;
        }

        var hotkey = char.ToLowerInvariant(hotkeyText[0]);
        if (char.IsWhiteSpace(hotkey) || hotkey is ':' or '/' or '\\' or '[' or ']' or '>' or ',' or '@' or '*')
        {
            ConsolePopup(player, "Invalid hotkey character.");
            return;
        }

        if (stationData.RadioData.Values.Any(data => data.Hotkey != '\0' && char.ToLowerInvariant(data.Hotkey) == hotkey))
        {
            ConsolePopup(player, "That hotkey is already in use by another channel.");
            return;
        }

        ProtoId<RadioChannelPrototype>? selectedChannel = null;
        foreach (var channel in CustomChannelPool)
        {
            if (!stationData.RadioData.ContainsKey(channel))
            {
                selectedChannel = channel;
                break;
            }
        }

        if (selectedChannel == null)
        {
            ConsolePopup(player, "Custom channel limit reached.");
            return;
        }

        stationData.RadioData[selectedChannel.Value] = new FactionRadioData(true)
        {
            IsCustom = true,
            CustomName = name,
            Hotkey = hotkey,
            CustomColor = ClampBrightCustomColor(args.Red, args.Green, args.Blue),
        };

        Dirty(station.Value, stationData);
        UpdateOrders(station.Value);
    }

    private void OnEditChannel(EntityUid uid, StationModificationConsoleComponent component, StationModificationEditChannel args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null)
            return;

        if (!Validate(uid, component, player, out _))
            return;

        if (!TryComp<StationDataComponent>(station, out var stationData))
        {
            ConsolePopup(player, "No Station Data Component!");
            return;
        }

        if (args.ChannelID == "Common")
        {
            ConsolePopup(player, "Common channel cannot be customized.");
            return;
        }

        if (!stationData.RadioData.TryGetValue(args.ChannelID, out var existing))
        {
            ConsolePopup(player, "Invalid Channel!");
            return;
        }

        var name = args.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ConsolePopup(player, "Channel name is required.");
            return;
        }

        if (name.Length > 24)
            name = name[..24];

        _protoMan.Resolve(args.ChannelID, out RadioChannelPrototype? channelProto);

        var currentEffectiveName = !string.IsNullOrWhiteSpace(existing.CustomName)
            ? existing.CustomName!
            : channelProto?.LocalizedName ?? string.Empty;

        var currentEffectiveHotkey = existing.Hotkey != '\0'
            ? char.ToLowerInvariant(existing.Hotkey)
            : char.ToLowerInvariant(channelProto?.KeyCode ?? '\0');

        var nameChanged = !string.Equals(currentEffectiveName, name, StringComparison.OrdinalIgnoreCase);
        if (nameChanged && stationData.RadioData.Any(pair =>
                pair.Key != args.ChannelID &&
                !string.IsNullOrWhiteSpace(pair.Value.CustomName) &&
                string.Equals(pair.Value.CustomName, name, StringComparison.OrdinalIgnoreCase)))
        {
            ConsolePopup(player, "A channel with that name already exists.");
            return;
        }

        var hotkeyText = args.Hotkey?.Trim() ?? string.Empty;
        if (hotkeyText.Length != 1)
        {
            ConsolePopup(player, "Hotkey must be exactly one character.");
            return;
        }

        var hotkey = char.ToLowerInvariant(hotkeyText[0]);
        if (char.IsWhiteSpace(hotkey) || hotkey is ':' or '/' or '\\' or '[' or ']' or '>' or ',' or '@' or '*')
        {
            ConsolePopup(player, "Invalid hotkey character.");
            return;
        }

        var hotkeyChanged = currentEffectiveHotkey == '\0' || currentEffectiveHotkey != hotkey;
        if (hotkeyChanged && stationData.RadioData.Any(pair =>
                pair.Key != args.ChannelID &&
                pair.Value.Hotkey != '\0' &&
                char.ToLowerInvariant(pair.Value.Hotkey) == hotkey))
        {
            ConsolePopup(player, "That hotkey is already in use by another channel.");
            return;
        }

        existing.IsCustom = true;
        existing.CustomName = name;
        existing.Hotkey = hotkey;
        existing.CustomColor = ClampBrightCustomColor(args.Red, args.Green, args.Blue);
        existing.Enabled = true;

        Dirty(station.Value, stationData);
        UpdateOrders(station.Value);
    }

    private void OnDeleteChannel(EntityUid uid, StationModificationConsoleComponent component, StationModificationDeleteChannel args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null)
            return;

        if (!Validate(uid, component, player, out _))
            return;

        if (!TryComp<StationDataComponent>(station, out var stationData))
        {
            ConsolePopup(player, "No Station Data Component!");
            return;
        }

        if (args.ChannelID == "Common")
        {
            ConsolePopup(player, "Common channel cannot be customized.");
            return;
        }

        if (!stationData.RadioData.TryGetValue(args.ChannelID, out var existing))
        {
            ConsolePopup(player, "Invalid Channel!");
            return;
        }

        if (!existing.IsCustom)
        {
            ConsolePopup(player, "Only customized channels can be deleted.");
            return;
        }

        if (CustomChannelPool.Contains(args.ChannelID))
        {
            stationData.RadioData.Remove(args.ChannelID);
        }
        else
        {
            existing.IsCustom = false;
            existing.CustomName = null;
            existing.CustomColor = null;
            existing.Hotkey = '\0';
            existing.Access.Clear();
        }

        Dirty(station.Value, stationData);
        UpdateOrders(station.Value);
    }

    private void OnToggleAccess(EntityUid uid, StationModificationConsoleComponent component, StationModificationToggleAssignmentAccess args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        if (args.ToggleState)
        {
            if (crewAssignment.AccessIDs.Contains(args.Access))
            {
                return;
            }
            else
            {
                crewAssignment.AccessIDs.Add(args.Access);
            }
        }
        else
        {
            if (crewAssignment.AccessIDs.Contains(args.Access))
            {
                crewAssignment.AccessIDs.Remove(args.Access);
            }
            else
            {
                return;
            }
        }
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);
    }
    private void OnToggleAssign(EntityUid uid, StationModificationConsoleComponent component, StationModificationToggleAssign args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        crewAssignment.CanAssign = !crewAssignment.CanAssign;
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);
    }

    private void OnToggleClaim(EntityUid uid, StationModificationConsoleComponent component, StationModificationToggleClaim args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        crewAssignment.CanClaim = !crewAssignment.CanClaim;
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);
    }

    private void OnToggleGenRec(EntityUid uid, StationModificationConsoleComponent component, StationModificationToggleGenRec args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (!TryComp(station, out CrewAssignmentsComponent? crewAssignments))
        {
            ConsolePopup(player, "No CrewAssignment Component!");
            return;
        }
        if (!crewAssignments.CrewAssignments.TryGetValue(args.AccessID, out var crewAssignment))
        {
            ConsolePopup(player, "Invalid Assignment!");
            return;
        }
        crewAssignment.CanEditGeneralRecord = !crewAssignment.CanEditGeneralRecord;
        Dirty((EntityUid)station, crewAssignments);
        UpdateOrders(station.Value);
    }


    private void OnChangeName(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeName args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        // No station to deduct from.
        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Owner == null || args.Owner == "") return;
        if (args.Owner.Length > 24)
        {
            ConsolePopup(player, "Exceeded Maximum Length of 24 Characters!");
            return;
        }
        _station2.RenameStation(station.Value, args.Owner);
        UpdateOrders(station.Value);

    }

    private void OnChangeFactionTag(EntityUid uid, StationModificationConsoleComponent component, StationModificationChangeFactionTag args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null)
            return;

        if (!Validate(uid, component, player, out var stationData) || stationData == null)
            return;

        // Normalize keeps this safe/consistent even if client-side filtering is bypassed.
        var normalized = StationDataComponent.NormalizeFactionTag(args.Tag);

        if (string.IsNullOrEmpty(normalized))
        {
            // Clearing custom value falls back to generated tag. Prevent collisions there too.
            var fallback = stationData.GetResolvedFactionTag(MetaData(station.Value).EntityName);
            if (FactionTagExistsOnAnotherStation(station.Value, fallback))
            {
                ConsolePopup(player, $"Faction tag '{fallback}' is already used by another faction.");
                return;
            }
        }
        else if (FactionTagExistsOnAnotherStation(station.Value, normalized))
        {
            ConsolePopup(player, $"Faction tag '{normalized}' is already used by another faction.");
            return;
        }

        stationData.FactionTag = string.IsNullOrEmpty(normalized) ? null : normalized;
        Dirty(station.Value, stationData);

        // Refresh issued IDs so the new tag appears right away instead of waiting
        // for players to reassign or regenerate their cards.
        if (stationData.UID != 0)
            _idCard.RefreshStationIds(stationData.UID);

        UpdateOrders(station.Value);
    }

    private bool FactionTagExistsOnAnotherStation(EntityUid station, string candidateTag)
    {
        var normalizedCandidate = StationDataComponent.NormalizeFactionTag(candidateTag);
        if (string.IsNullOrEmpty(normalizedCandidate))
            return false;

        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var otherStation, out var otherData))
        {
            if (otherStation == station)
                continue;

            var otherResolved = otherData.GetResolvedFactionTag(MetaData(otherStation).EntityName);
            if (string.IsNullOrEmpty(otherResolved))
                continue;

            if (string.Equals(otherResolved, normalizedCandidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
    private void OnAddOwner(EntityUid uid, StationModificationConsoleComponent component, StationModificationAddOwner args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        // No station to deduct from.
        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Owner == null || args.Owner == "") return;
        if (stationData!.IsOwner(args.Owner))
        {
            ConsolePopup(args.Actor, "That owner already exists.");
            return;
        }
        if (args.Owner == null || args.Owner == "") return;
        stationData.AddOwner(args.Owner);
        Dirty((EntityUid)station, stationData);
        UpdateOrders(station.Value);
    }
    private void OnAddAccess(EntityUid uid, StationModificationConsoleComponent component, StationModificationAddAccess args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        // No station to deduct from.
        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Owner == null || args.Owner == "") return;
        // No station to deduct from.
        if (!TryComp(station, out CrewAccessesComponent? crewAccesses))
        {
            ConsolePopup(player, "No CrewAccesses Component!");
            stationData = null;
            return;
        }
        if (crewAccesses.CrewAccesses.ContainsKey(args.Owner))
        {
            ConsolePopup(args.Actor, "That access already exists.");
            return;
        }
        if (args.Owner == null || args.Owner == "") return;
        if (args.Owner.Length > 24)
        {
            ConsolePopup(player, "Exceeded Maximum Length of 24 Characters!");
            return;
        }
        crewAccesses.CreateAccess(args.Owner);
        Dirty((EntityUid)station, crewAccesses);
        UpdateOrders(station.Value);
    }

    private void OnDeleteAccess(EntityUid uid, StationModificationConsoleComponent component, StationModificationRemoveAccess args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var station = _station.GetOwningStation(uid);
        if (station == null) return;

        if (!Validate(uid, component, player, out var stationData)) return;
        if (args.Owner == null || args.Owner == "") return;
        if (!TryComp(station, out CrewAccessesComponent? crewAccesses))
        {
            ConsolePopup(player, "No CrewAccesses Component!");
            stationData = null;
            return;
        }
        if (args.Owner == null || args.Owner == "") return;
        crewAccesses.RemoveAccess(args.Owner);
        Dirty((EntityUid)station, crewAccesses);
        UpdateOrders(station.Value);
    }



    private void OnOrderUIOpened(EntityUid uid, StationModificationConsoleComponent component, BoundUIOpenedEvent args)
    {
        var station = _station.GetOwningStation(uid);
        UpdateOrderState(uid, station);
    }
    public void UpdateAllUserInterfaces()
    {
        var query = EntityQueryEnumerator<StationModificationConsoleComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            var station = _station.GetOwningStation(uid);
            if (station == null) continue;
            UpdateOrderState(uid, station);
        }
    }

    #endregion

    private void UpdateOrderState(EntityUid consoleUid, EntityUid? station)
    {
        if (!TryComp<StationDataComponent>(station, out var data))
            return;
        if (!TryComp<StationModificationConsoleComponent>(consoleUid, out var console))
            return;
        if (!TryComp<CrewAccessesComponent>(station, out var cadata))
            return;
        if (!TryComp<CrewAssignmentsComponent>(station, out var casdata))
            return;
        if (!TryComp<StationBankAccountComponent>(station, out var bank) || bank == null)
            return;
        if (_uiSystem.HasUi(consoleUid, StationModUiKey.StationMod))
        {
            bool hasTrade = false;
            if (_station2.GetStationTradeStation(station.Value) != null)
            {
                hasTrade = true;
            }
            List<ProtoId<ResonanceProductPrototype>> resonanceMarket = new();
            Dictionary<ProtoId<ResonanceProductPrototype>, int> partialPurchases = new();
            if (hasTrade && _crewMeta.MetaRecords != null)
            {
                resonanceMarket = BuildResonanceMarket();
                partialPurchases = _crewMeta.MetaRecords.PartialPurchases;
            }

            _uiSystem.SetUiState(consoleUid,
                StationModUiKey.StationMod,
                new StationModificationInterfaceState(
                MetaData(station!.Value).EntityName,
                data.GetResolvedFactionTag(MetaData(station.Value).EntityName),
                GetNetEntity(station.Value),
                data.Owners,
                cadata.CrewAccesses,
                casdata.CrewAssignments,
                data.ImportTax,
                data.ExportTax,
                data.SalesTax,
                data.Level,
                _cargo.GetBalanceFromAccount((station.Value, bank), "Cargo"),
                data.RadioData,
                data.JobNetEnabled,
                hasTrade,
                resonanceMarket,
                data.StoredResonance,
                partialPurchases,
                data.UpgradeProgress
            ));
        }
    }

    private List<ProtoId<ResonanceProductPrototype>> BuildResonanceMarket()
    {
        if (_crewMeta.MetaRecords == null) return new();
        var meta = _crewMeta.MetaRecords;

        List<ProtoId<ResonanceProductPrototype>> final = new();
        var protos = _protoMan.EnumeratePrototypes<ResonanceProductPrototype>();
        foreach (var product in protos)
        {
            if (product.Unique && meta.ResonancePurchases.Contains(product.ID)) continue;
            if (product.Requires != null && !meta.ResonancePurchases.Contains(product.Requires.Value)) continue;
            final.Add(product.ID);
        }
        return final;
    }

    private void ConsolePopup(EntityUid actor, string text)
    {
        _popup.PopupCursor(text, actor);
    }


    private void UpdateOrders(EntityUid dbUid)
    {
        // Order added so all consoles need updating.
        var orderQuery = AllEntityQuery<StationModificationConsoleComponent>();

        while (orderQuery.MoveNext(out var uid, out var _))
        {
            var station = _station.GetOwningStation(uid);
            if (station != dbUid)
                continue;

            UpdateOrderState(uid, station);
        }
    }
}
/// <summary>
/// Directed event fired on a station when an assignment is made.
/// </summary>
[PublicAPI]
public sealed class StationCreateAssignment : EntityEventArgs
{
    /// <summary>
    /// ID of the station the assignment was added to.
    /// </summary>
    public EntityUid StationId;

    public StationCreateAssignment(EntityUid stationId)
    {
        StationId = stationId;
    }
}

/// <summary>
/// Directed event fired on a station when an assignment is removed.
/// </summary>
[PublicAPI]
public sealed class StationDeleteAssignment : EntityEventArgs
{
    /// <summary>
    /// ID of the station the assignment was removed from.
    /// </summary>
    public EntityUid StationId;

    public StationDeleteAssignment(EntityUid stationId)
    {
        StationId = stationId;
    }
}

/// <summary>
/// Directed event fired on a station when an assignment is renamed.
/// </summary>
[PublicAPI]
public sealed class StationRenameAssignment : EntityEventArgs
{
    /// <summary>
    /// ID of the stations renamed assignment.
    /// </summary>
    public EntityUid StationId;

    public StationRenameAssignment(EntityUid stationId)
    {
        StationId = stationId;
    }
}
