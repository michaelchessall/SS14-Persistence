using Content.Server._NF.Bank;
using Content.Server.Access.Systems;
using Content.Server.Chat.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Coordinates;
using Content.Shared.Invoices.Components;
using Content.Shared.Invoices.Systems;
using Content.Shared.Station.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Server.Invoices.Systems;

[UsedImplicitly]
public sealed class InvoicePrinterConsoleSystem : SharedInvoicePrinterConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, InvoicePrinterStationSelectMessage>(OnSelectStation);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, PrintInvoice>(Print);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, ChangeInvoiceMode>(ToggleMode);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, ChangeInvoicePayslipMode>(TogglePayslipMode);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        Subs.BuiEvents<InvoicePrinterConsoleComponent>(InvoicePrinterConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
        });
        SubscribeLocalEvent<InvoiceComponent, PayInvoice>(OnPayInvoice);
        SubscribeLocalEvent<InvoiceComponent, PayInvoicePersonal>(OnPayInvoicePersonal);
        Subs.BuiEvents<InvoiceComponent>(InvoiceUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
        });

    }

    private void OnSelectStation(EntityUid uid, InvoicePrinterConsoleComponent component, InvoicePrinterStationSelectMessage args)
    {
        component.SelectedStation = args.Target;
        UpdateUserInterface(uid, component, args);
    }

    private void Print(EntityUid uid, InvoicePrinterConsoleComponent component, PrintInvoice args)
    {
        if (!(args.InvoiceCost > 0)) return;
        var privilegedIdName = string.Empty;
        var privilegedName = string.Empty;
        int taxRate = 0;
        int owningStation = 0;
        if (args.Actor is not { Valid: true } player)
            return;
        int? targetStation = null;
        string? targetPerson = null;
        bool payslipMode = false;
        string entityName = "invoice";

        if (component.StationMode)
        {
            if (!component.InvoiceMode)
            {
                payslipMode = true;
                entityName = "payslip";
            }
            var printingStation = _station.GetStationByID(component.SelectedStation);
            if (printingStation == null) return;
            if (!TryComp<StationDataComponent>(printingStation, out var printingData) || printingData == null) return;
            if (printingData.StationName == null) return;
            privilegedName = printingData.StationName;
            targetStation = printingData.UID;
            var taxingStation = _station.GetOwningStation(uid, null, true);
            if (taxingStation != null)
            {
                if (TryComp<StationDataComponent>(taxingStation, out var sD) && sD != null)
                {
                    taxRate = sD.SalesTax;
                    if (component.StationMode)
                    {
                        owningStation = sD.UID;
                    }
                }
            }
        }
        else
        {
            var taxingStation = _station.GetOwningStation(uid, null, true);
            if (taxingStation != null)
            {
                if (TryComp<StationDataComponent>(taxingStation, out var sD) && sD != null)
                {
                    taxRate = sD.SalesTax;
                    owningStation = sD.UID;

                }
            }
            if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
            {
                privilegedIdName = Comp<MetaDataComponent>(idCard).EntityName;
                if (TryComp<IdCardComponent>(idCard, out var id) && id.FullName != null)
                {
                    privilegedName = id.FullName;
                    targetPerson = id.FullName;

                }
                else return;
            }
            else return;
        }

        if (payslipMode)
        {
            if (targetStation == null) return;
            var station = _station.GetStationByID(targetStation.Value);
            if (station == null) return;
            var realName = string.Empty;
            if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
            {
                privilegedIdName = Comp<MetaDataComponent>(idCard).EntityName;
                if (TryComp<IdCardComponent>(idCard, out var id) && id.FullName != null)
                {
                    realName = id.FullName;

                }
                else return;
            }
            if (realName == string.Empty) return;
            if (_station.CanSpend(realName, station.Value, args.InvoiceCost))
            {
                var accountBalance = 0;
                if (TryComp<StationBankAccountComponent>(station, out var stationBank) && stationBank != null)
                {
                    accountBalance = _cargo.GetBalanceFromAccount((station.Value, stationBank), "Cargo");
                }
                else
                {
                    return;
                }
                if (accountBalance < args.InvoiceCost)
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", args.InvoiceCost)));
                    return;
                }
                _cargo.UpdateBankAccount((station.Value, stationBank), -args.InvoiceCost, "Cargo");
                _station.TrackSpending(realName, station.Value, args.InvoiceCost);
                var invoice = _entityManager.SpawnAtPosition("Invoice", player.ToCoordinates());

                if (!_hands.TryPickupAnyHand(player, invoice))
                    _transform.SetLocalRotation(invoice, Angle.Zero); // Orient these to grid north instead of map north
                if (TryComp<InvoiceComponent>(invoice, out var invoiceComp) && invoiceComp != null)
                {
                    invoiceComp.TargetPerson = targetPerson;
                    invoiceComp.TargetStation = targetStation;
                    invoiceComp.InvoiceCost = args.InvoiceCost;
                    invoiceComp.InvoiceReason = args.InvoiceReason;
                    invoiceComp.PayslipMode = true;
                    _metaSystem.SetEntityName(invoice, $"{entityName} ${args.InvoiceCost} {args.InvoiceTitle}");
                    Dirty(invoice, invoiceComp);
                    _audio.PlayEntity(component.PrintSound, args.Actor, uid);
                }
            }
        }
        else
        {
            var invoice = _entityManager.SpawnAtPosition("Invoice", player.ToCoordinates());

            if (!_hands.TryPickupAnyHand(player, invoice))
                _transform.SetLocalRotation(invoice, Angle.Zero); // Orient these to grid north instead of map north
            if (TryComp<InvoiceComponent>(invoice, out var invoiceComp) && invoiceComp != null)
            {
                invoiceComp.TargetPerson = targetPerson;
                invoiceComp.TargetStation = targetStation;
                invoiceComp.InvoiceCost = args.InvoiceCost;
                invoiceComp.InvoiceReason = args.InvoiceReason;
                _metaSystem.SetEntityName(invoice, $"{entityName} ${args.InvoiceCost} {args.InvoiceTitle} ");
                Dirty(invoice, invoiceComp);
                _audio.PlayEntity(component.PrintSound, args.Actor, uid);
            }
        }

        UpdateUserInterface(uid, component, args);
    }

    private void TogglePayslipMode(EntityUid uid, InvoicePrinterConsoleComponent component, ChangeInvoicePayslipMode args)
    {
        if (args.Actor is not { Valid: true } player)
            return;
        component.InvoiceMode = !component.InvoiceMode;
        UpdateUserInterface(uid, component, args);
    }

    private void ToggleMode(EntityUid uid, InvoicePrinterConsoleComponent component, ChangeInvoiceMode args)
    {
        if (args.Actor is not { Valid: true } player)
            return;
        component.StationMode = !component.StationMode;
        UpdateUserInterface(uid, component, args);
    }
    private void UpdateUserInterface(EntityUid uid, InvoicePrinterConsoleComponent component, EntityEventArgs args)
    {
        var privilegedIdName = string.Empty;
        var privilegedName = string.Empty;
        bool idPresent = false;
        int taxRate = 0;
        if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
        {
            idPresent = true;
            privilegedIdName = Comp<MetaDataComponent>(idCard).EntityName;
            if (TryComp<IdCardComponent>(idCard, out var id) && id.FullName != null)
            {
                privilegedName = id.FullName;
            }

        }
        var taxStation = _station.GetOwningStation(uid, null, true);
        int taxingStation = 0;
        string taxingName = "Unknown";
        if (taxStation != null)
        {
            if (TryComp<StationDataComponent>(taxStation, out var sD) && sD != null)
            {
                taxingStation = sD.UID;
                taxRate = sD.SalesTax;
                if (sD.StationName != null)
                {
                    taxingName = sD.StationName;
                }
            }
            else
            {
                component.StationMode = false;
            }
        }
        List<EntityUid> possibleStations = new();
        Dictionary<int, string> formattedStations = new();
        if (privilegedName != string.Empty && privilegedName != null)
        {
            possibleStations = _station.GetStationsAvailableTo(privilegedName);
        }
        foreach (var station in possibleStations)
        {
            if (TryComp<StationDataComponent>(station, out var data) && data != null)
            {
                if (data.StationName != null)
                {
                    formattedStations.Add(data.UID, data.StationName);
                }
            }
        }
        var selectedName = "None";
        if (component.SelectedStation != 0)
        {
            var selectedStation = _station.GetStationByID(component.SelectedStation);
            if (selectedStation != null)
            {
                if (TryComp<StationDataComponent>(selectedStation, out var selectedData) && selectedData != null)
                {
                    if (selectedData.StationName != null)
                        selectedName = selectedData.StationName;
                }
            }
        }
        InvoicePrinterConsoleBoundUserInterfaceState newState = new(idPresent, privilegedIdName, privilegedName, component.StationMode, taxRate, taxingStation, taxingName, component.SelectedStation, formattedStations, selectedName, !component.StationMode || component.InvoiceMode);
        _userInterface.SetUiState(uid, InvoicePrinterConsoleUiKey.Key, newState);
    }


    private void UpdateUserInterface(EntityUid uid, InvoiceComponent component, BaseBoundUserInterfaceEvent args)
    {
        Dictionary<int, string> possibleStations = new();
        var stations = _station.GetStations();
        var userName = Name(args.Actor);
        string paidTo = "";
        if (component.TargetPerson != null)
        {
            paidTo = component.TargetPerson;
        }
        if (!component.PayslipMode)
        {
            foreach (var station in stations)
            {
                if (TryComp<StationDataComponent>(station, out var sD) && sD != null)
                {
                    if (component.TargetStation != null)
                    {
                        if (component.TargetStation == sD.UID && sD.StationName != null) paidTo = sD.StationName;
                    }
                    if (_station.CanSpend(userName, station, component.InvoiceCost))
                    {
                        possibleStations.Add(sD.UID, sD.StationName != null ? sD.StationName : "");
                    }
                }
            }
        }
        else
        {
            foreach (var station in stations)
            {
                if (TryComp<StationDataComponent>(station, out var sD) && sD != null)
                {
                    if (component.TargetStation != null)
                    {
                        if (component.TargetStation == sD.UID && sD.StationName != null) paidTo = sD.StationName;
                    }
                    if (_station.HasRecord(userName, station))
                    {
                        possibleStations.Add(sD.UID, sD.StationName != null ? sD.StationName : "");
                    }
                }
            }
        }

        InvoiceBoundUserInterfaceState newState = new(possibleStations, component.InvoiceCost, component.InvoiceReason, paidTo, component.PaidBy, component.Paid, userName, component.PayslipMode, component.PaidTime);
        _userInterface.SetUiState(uid, InvoiceUiKey.Key, newState);
    }

    private void OnPayInvoice(EntityUid uid, InvoiceComponent component, PayInvoice args)
    {
        if (component.Paid) return;
        var station = _station.GetStationByID(args.Station);
        var userName = Name(args.Actor);
        var cost = component.InvoiceCost;
        var stationName = "";
        var taxAmount = 0;
        bool valid = false;
        EntityUid? taxStation = null;
        if (!component.PayslipMode)
        {
            if (station != null)
            {
                if (component.TaxOwner != 0)
                {
                    taxStation = _station.GetStationByID(component.TaxOwner);
                    if (taxStation != null)
                    {
                        if (TryComp<StationDataComponent>(taxStation, out var taxSD) && taxSD != null)
                        {
                            if (taxSD.SalesTax > 0)
                            {
                                var taxRate = taxSD.SalesTax;
                                taxAmount = (int)Math.Round((float)cost * ((float)taxRate / 100f));
                            }
                        }
                    }

                }
                if (TryComp<StationDataComponent>(station, out var sD) && sD != null)
                {
                    if (sD.StationName != null) stationName = sD.StationName;
                    if (_station.CanSpend(userName, station.Value, component.InvoiceCost))
                    {
                        valid = true;
                    }
                }
            }
            if (valid && station != null)
            {
                var accountBalance = 0;
                if (TryComp<StationBankAccountComponent>(station, out var stationBank) && stationBank != null)
                {
                    accountBalance = _cargo.GetBalanceFromAccount((station.Value, stationBank), "Cargo");
                    // Not enough balance
                    if (cost > accountBalance)
                    {
                        ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                        _audio.PlayEntity(component.ErrorSound, args.Actor, uid);
                        return;
                    }

                    _cargo.UpdateBankAccount((station.Value, stationBank), -cost, "Cargo");
                    if (taxAmount > 0 && taxStation != null)
                    {
                        if (TryComp<StationBankAccountComponent>(taxStation, out var taxBank))
                        {
                            _cargo.UpdateBankAccount((taxStation.Value, taxBank), taxAmount, "Cargo");
                            cost -= taxAmount;
                        }
                    }
                    if (component.TargetStation != null)
                    {
                        var target = _station.GetStationByID(component.TargetStation.Value);
                        if (target != null)
                        {
                            if (TryComp<StationBankAccountComponent>(target, out var targetAccount) && targetAccount != null)
                            {
                                _cargo.UpdateBankAccount((target.Value, targetAccount), cost, "Cargo");
                            }
                        }

                    }
                    else if (component.TargetPerson != null)
                    {
                        var target = component.TargetPerson;
                        _bank.TryBankDeposit(target, cost);
                    }
                    component.Paid = true;
                    component.PaidBy = $"{stationName} ({userName})";
                    component.PaidTime = DateTime.Now;
                    _station.TrackSpending(userName, station.Value, component.InvoiceCost);
                    _audio.PlayEntity(component.PaySuccessSound, args.Actor, uid);
                    _appearance.SetData(uid, PaperVisuals.Invoice, "paid");
                }
            }
        }
        else
        {
            if (station != null)
            {
                if (TryComp<StationBankAccountComponent>(station, out var stationBank) && stationBank != null)
                {
                    if (TryComp<StationDataComponent>(station, out var sD) && sD != null)
                    {
                        if (sD.StationName != null) stationName = sD.StationName;
                    }
                    _cargo.UpdateBankAccount((station.Value, stationBank), cost, "Cargo");
                    component.Paid = true;
                    component.PaidBy = $"{stationName} ({userName})";
                    component.PaidTime = DateTime.Now;
                    _audio.PlayEntity(component.PaySuccessSound, args.Actor, uid);
                    _appearance.SetData(uid, PaperVisuals.Invoice, "paid");
                }
            }
        }
        UpdateUserInterface(uid, component, args);
    }

    private void ConsolePopup(EntityUid actor, string text)
    {
        _popup.PopupCursor(text, actor);

    }


    private void OnPayInvoicePersonal(EntityUid uid, InvoiceComponent component, PayInvoicePersonal args)
    {
        if (component.Paid) return;
        var cost = component.InvoiceCost;
        var userName = Name(args.Actor);
        var taxAmount = 0;
        EntityUid? taxStation = null;
        if (!component.PayslipMode)
        {
            if (component.TaxOwner != 0)
            {
                taxStation = _station.GetStationByID(component.TaxOwner);
                if (taxStation != null)
                {
                    if (TryComp<StationDataComponent>(taxStation, out var taxSD) && taxSD != null)
                    {
                        if (taxSD.SalesTax > 0)
                        {
                            var taxRate = taxSD.SalesTax;
                            taxAmount = (int)Math.Round((float)cost * ((float)taxRate / 100f));
                        }
                    }
                }
            }
            if (_bank.TryBankWithdraw(args.Actor, component.InvoiceCost))
            {
                if (taxAmount > 0 && taxStation != null)
                {
                    if (TryComp<StationBankAccountComponent>(taxStation, out var taxBank))
                    {
                        _cargo.UpdateBankAccount((taxStation.Value, taxBank), taxAmount, "Cargo");
                        cost -= taxAmount;
                    }
                }
                component.Paid = true;
                component.PaidBy = userName;
                component.PaidTime = DateTime.Now;
                _audio.PlayEntity(component.PaySuccessSound, args.Actor, uid);
                _appearance.SetData(uid, PaperVisuals.Invoice, "paid");
                if (component.TargetStation != null)
                {
                    var target = _station.GetStationByID(component.TargetStation.Value);
                    if (target != null)
                    {
                        if (TryComp<StationBankAccountComponent>(target, out var targetAccount) && targetAccount != null)
                        {
                            _cargo.UpdateBankAccount((target.Value, targetAccount), cost, "Cargo");
                        }
                    }

                }
                else if (component.TargetPerson != null)
                {
                    var target = component.TargetPerson;
                    _bank.TryBankDeposit(target, cost);
                }

            }
            else
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                _audio.PlayEntity(component.ErrorSound, args.Actor, uid);
                return;
            }
        }
        else
        {
            if (_bank.TryBankDeposit(args.Actor, cost))
            {
                component.Paid = true;
                component.PaidBy = userName;
                component.PaidTime = DateTime.Now;
                _audio.PlayEntity(component.PaySuccessSound, args.Actor, uid);
                _appearance.SetData(uid, PaperVisuals.Invoice, "paid");
            }
        }
        UpdateUserInterface(uid, component, args);
    }
}
