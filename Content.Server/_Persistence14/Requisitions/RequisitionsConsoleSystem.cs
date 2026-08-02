using System.Linq;
using System.Text;
using Content.Server.Construction;
using Content.Server.Lathe;
using Content.Server.Materials;
using Content.Shared._Persistence14.Requisitions;
using Content.Shared.Construction.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Invoices.Components;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Materials.OreSilo;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Research.Prototypes;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Persistence14.Requisitions;

/// <inheritdoc/>
public sealed class RequisitionsConsoleSystem : SharedRequisitionsConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly LatheSystem _lathe = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlatpackSystem _flatpack = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private EntityQuery<LatheComponent> _latheQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RequisitionsConsoleComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<RequisitionsConsoleComponent, RequisitionCheckoutMessage>(OnCheckout);
        SubscribeLocalEvent<RequisitionsConsoleComponent, RequisitionCancelMessage>(OnCancel);
        SubscribeLocalEvent<RequisitionsConsoleComponent, RequisitionEjectFlatpacksMessage>(OnEjectFlatpacks);
        SubscribeLocalEvent<RequisitionsConsoleComponent, MaterialAmountChangedEvent>(OnMaterialChanged);
        SubscribeLocalEvent<RequisitionsLatheJobComponent, LatheItemProducedEvent>(OnLatheItemProduced);
        // Run after the lathe's own power handler so its abort/refund has already returned materials to it.
        SubscribeLocalEvent<RequisitionsLatheJobComponent, PowerChangedEvent>(OnJobLathePowerChanged, after: new[] { typeof(LatheSystem) });
        SubscribeLocalEvent<RequisitionsLatheJobComponent, ComponentShutdown>(OnJobLatheShutdown);

        _latheQuery = GetEntityQuery<LatheComponent>();
    }

    /// <summary>The whole console is access-restricted: unauthorised players can't even open it.</summary>
    private void OnOpenAttempt(Entity<RequisitionsConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || HasConfigAccess(ent, args.User))
            return;

        args.Cancel();
        if (!args.Silent)
            _popup.PopupEntity(Loc.GetString("requisitions-access-denied"), ent.Owner, args.User);
    }

    /// <summary>Materials inserted/removed (e.g. a customer contributing sheets) — refresh the open UI live.</summary>
    private void OnMaterialChanged(Entity<RequisitionsConsoleComponent> ent, ref MaterialAmountChangedEvent args)
    {
        UpdateUi(ent);
    }

    #region Checkout

    private void OnCheckout(Entity<RequisitionsConsoleComponent> ent, ref RequisitionCheckoutMessage args)
    {
        RefreshLinkState(ent);

        if (args.Items.Count == 0)
            return;

        // One checkout at a time: refuse while a previous order is still printing.
        if (ent.Comp.OutstandingJobs > 0)
        {
            _popup.PopupEntity(Loc.GetString("requisitions-processing"), ent.Owner, args.Actor);
            return;
        }

        // Inserted sheets the customer is contributing: they discount the bill and physically feed the print.
        var pool = _materialStorage.GetStoredMaterials(ent.Owner, localOnly: true)
            .ToDictionary(kv => kv.Key.Id, kv => kv.Value);

        var invoiceMode = args.PrintInvoice;
        var detailedInvoice = ent.Comp.DetailedInvoice;
        var runningCost = 0;
        var producedAny = false;
        var anyFailed = false;

        // Invoice body accumulation (only populated when printing an invoice).
        var itemsBody = new StringBuilder();
        var invoiceFees = new Dictionary<string, (string Name, bool Percent, int Rate, int Count, int Total)>();
        var invoiceMats = new Dictionary<string, (int Raw, int Covered, int Billed)>();
        var invoiceMatBilled = 0;
        var invoiceMatWorth = 0;

        foreach (var item in args.Items)
        {
            // A single bad line must never abort the whole order.
            try
            {
                if (!Proto.TryIndex<LatheRecipePrototype>(item.RecipeId, out var recipe))
                {
                    if (invoiceMode)
                        AppendInvoiceFailure(itemsBody, item.RecipeId, Loc.GetString("requisitions-fail-unknown"));
                    anyFailed = true;
                    continue;
                }

                var qty = Math.Max(1, item.Quantity);
                var flatpack = item.Flatpack && ent.Comp.FlatpackerLinked && IsFlatpackable(recipe);
                var mult = flatpack ? ent.Comp.FlatpackMaterialMultiplier : 1f;

                // Per-unit raw material need (before the customer's contribution).
                var perUnit = new Dictionary<string, int>();
                foreach (var (mat, baseAmount) in recipe.Materials)
                    perUnit[mat.Id] = (int) MathF.Ceiling(baseAmount * mult);

                // Dispatch one unit at a time and only bill for what actually queues. A machine can accept fewer
                // than requested — it runs out of materials partway, hits its per-request limit, or a researched
                // recipe has fewer prints left — and the unqueued units must never reach the invoice.
                string? failReason;
                var coverUsed = new Dictionary<string, int>();
                var queued = flatpack
                    ? TryDispatchFlatpack(ent, recipe, qty, perUnit, pool, coverUsed, out failReason)
                    : TryDispatchLathe(ent, recipe, qty, perUnit, pool, coverUsed, out failReason);

                if (queued <= 0)
                {
                    if (invoiceMode)
                        AppendInvoiceFailure(itemsBody, Lathe.GetRecipeName(recipe), failReason ?? Loc.GetString("requisitions-fail-no-materials"));
                    anyFailed = true;
                    continue;
                }

                if (queued < qty)
                    anyFailed = true; // only part of this line could be produced

                // Bill for the units that actually queued: their material need, minus the customer's contribution
                // that was applied to them, plus fees on the units' full material worth.
                var itemCost = 0;
                var worth = 0;
                var matLines = new List<(string Mat, int Raw, int Covered, int Billed)>();
                foreach (var (mat, need) in perUnit)
                {
                    var raw = need * queued;
                    var covered = coverUsed.GetValueOrDefault(mat);
                    var billed = SheetCost(ent.Comp, mat, raw - covered);
                    itemCost += billed;
                    worth += SheetCost(ent.Comp, mat, raw);
                    matLines.Add((mat, raw, covered, billed));
                }

                var feeLines = new List<(RequisitionFee Fee, int Amount)>();
                foreach (var fee in FeesFor(ent.Comp, item.RecipeId, flatpack))
                {
                    var amt = fee.AmountFor(worth, queued);
                    itemCost += amt;
                    feeLines.Add((fee, amt));
                }

                runningCost += itemCost;
                producedAny = true;

                // Build this item's invoice section as we go. Every item gets its "name — cost" line; the
                // per-material and per-fee breakdown (and the totals it feeds) is only built for a detailed invoice.
                if (invoiceMode)
                {
                    var name = Lathe.GetRecipeName(recipe);
                    var label = queued < qty ? $"{name} (×{queued} of {qty})" : name;
                    itemsBody.Append($"[bold][color=#9a6a12]{label}[/color][/bold]   ${itemCost}\n");

                    if (detailedInvoice)
                    {
                        foreach (var (mat, raw, covered, billed) in matLines)
                        {
                            invoiceMatBilled += billed;
                            invoiceMatWorth += SheetCost(ent.Comp, mat, raw);
                            var disc = covered > 0 ? $"  (−{SheetLabelServer(mat, covered)})" : "";
                            itemsBody.Append($"    {SheetLabelServer(mat, raw)}{disc}   ${billed}\n");

                            // Aggregate for the per-material totals list.
                            var prevMat = invoiceMats.GetValueOrDefault(mat);
                            invoiceMats[mat] = (prevMat.Raw + raw, prevMat.Covered + covered, prevMat.Billed + billed);
                        }
                        foreach (var (fee, amt) in feeLines)
                        {
                            var rate = fee.Type == RequisitionFeeType.Percent ? $"{fee.Price}%" : $"${fee.Price}";
                            itemsBody.Append($"    {fee.Name} ({rate})   ${amt}\n");
                            var prev = invoiceFees.GetValueOrDefault(fee.Id);
                            invoiceFees[fee.Id] = (fee.Name, fee.Type == RequisitionFeeType.Percent, fee.Price, prev.Count + queued, prev.Total + amt);
                        }
                        itemsBody.Append('\n');
                    }
                    else
                    {
                        // Trimmed invoice: still separate each item with a blank line, like the failure lines do.
                        itemsBody.Append('\n');
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Requisitions] checkout of '{item.RecipeId}' threw, continuing with the rest: {e}");
                if (invoiceMode)
                    AppendInvoiceFailure(itemsBody, item.RecipeId, Loc.GetString("requisitions-fail-error"));
                anyFailed = true;
            }
        }

        if (!producedAny)
        {
            _popup.PopupEntity(Loc.GetString("requisitions-checkout-failed"), ent.Owner, args.Actor);
            return;
        }

        // Return any inserted sheets the order didn't consume.
        foreach (var leftover in _materialStorage.EjectAllMaterial(ent.Owner))
            _hands.PickupOrDrop(args.Actor, leftover);

        // When asked, print a payable invoice for the order — paid later via bank into the owning faction.
        // A checkout without an invoice simply prints the items at no charge.
        if (invoiceMode)
        {
            var body = BuildInvoiceBody(args.InvoiceTitle, itemsBody, invoiceMats, invoiceFees, invoiceMatBilled, invoiceMatWorth, runningCost, detailedInvoice);
            SpawnInvoice(ent, args.Actor, args.InvoiceTitle, runningCost, body);
        }

        _popup.PopupEntity(
            Loc.GetString(anyFailed ? "requisitions-checkout-partial" : "requisitions-checkout-done"),
            ent.Owner, args.Actor);

        UpdateUi(ent, args.Actor);
    }

    /// <summary>Assembles the invoice body markup. A detailed invoice has a header, per-item detail, then a totals
    /// section (per-material list, material-cost summary, fees, grand total). A non-detailed one is just the title,
    /// one "name — cost" line per item (plus any failures), and the grand total.</summary>
    private string BuildInvoiceBody(string title, StringBuilder items,
        Dictionary<string, (int Raw, int Covered, int Billed)> mats,
        Dictionary<string, (string Name, bool Percent, int Rate, int Count, int Total)> fees,
        int matBilled, int matWorth, int total, bool detailed)
    {
        if (string.IsNullOrWhiteSpace(title))
            title = Loc.GetString("requisitions-invoice-default-title");

        // Colours are tuned for the light "paper" background the invoice renders on: deep, saturated tones.
        // Title = deep indigo, section headers = dark teal, materials = neutral, the material subtotal = brown,
        // fees = violet, grand total = green, failures (elsewhere) = dark red.
        var sb = new StringBuilder();
        sb.Append($"[head=2][color=#2a3f6a]{title}[/color][/head]\n\n");

        // Trimmed invoice: just the per-item lines and the grand total.
        if (!detailed)
        {
            sb.Append(items);
            sb.Append('\n');
            sb.Append($"[bold][color=#1f7a33]{Loc.GetString("requisitions-summary-total")}: ${total}[/color][/bold]");
            return sb.ToString();
        }

        sb.Append($"[head=3][color=#1f6f5c]{Loc.GetString("requisitions-invoice-items")}[/color][/head]\n");
        sb.Append(items);
        sb.Append($"[head=3][color=#1f6f5c]{Loc.GetString("requisitions-invoice-total-header")}[/color][/head]\n");

        // Per-material totals across the whole order, like the console's stock/breakdown lines.
        foreach (var (mat, tally) in mats.OrderBy(kv => kv.Key))
        {
            var disc = tally.Covered > 0 ? $"  [color=#2f7a3a](−{SheetLabelServer(mat, tally.Covered)})[/color]" : "";
            sb.Append($"{SheetLabelServer(mat, tally.Raw)}{disc}   ${tally.Billed}\n");
        }

        sb.Append($"[bold][color=#7a4a12]{Loc.GetString("requisitions-summary-material")}[/color][/bold]: ${matBilled}");
        if (matWorth > matBilled)
            sb.Append($"  [color=#2f7a3a](−${matWorth - matBilled} {Loc.GetString("requisitions-invoice-your-materials")})[/color]");
        sb.Append('\n');

        foreach (var (_, f) in fees)
        {
            var rate = f.Percent ? $"{f.Rate}%" : $"${f.Rate}";
            sb.Append($"[color=#5e3a8c]{f.Name} ({rate}) ({f.Count})[/color]   ${f.Total}\n");
        }

        sb.Append($"[bold][color=#1f7a33]{Loc.GetString("requisitions-summary-total")}: ${total}[/color][/bold]");
        return sb.ToString();
    }

    /// <summary>Appends a bold-red "failed" line (with a reason) for an item the order couldn't fulfil.</summary>
    private void AppendInvoiceFailure(StringBuilder body, string name, string reason)
    {
        body.Append($"[bold][color=#b32020]{name} — {Loc.GetString("requisitions-invoice-failed")}[/color][/bold]\n");
        body.Append($"    [color=#b32020]{reason}[/color]\n\n");
    }

    /// <summary>Spawns a payable invoice item targeted at the faction the console is tagged to and hands it over.</summary>
    private void SpawnInvoice(Entity<RequisitionsConsoleComponent> console, EntityUid actor, string title, int cost, string body)
    {
        var invoice = Spawn("Invoice", Transform(console).Coordinates);

        if (TryComp<InvoiceComponent>(invoice, out var comp))
        {
            comp.InvoiceCost = cost;
            comp.InvoiceReason = body;

            // Pay into the faction the console was tagged to with the station/faction tagger (a StationTracker
            // pointing at that station). Fall back to whatever station owns the console's grid when untagged.
            EntityUid? station = null;
            if (TryComp<StationTrackerComponent>(console.Owner, out var tracker) && tracker.Station is { } tagged)
                station = tagged;
            station ??= _station.GetOwningStation(console.Owner, null, true);

            if (station != null && TryComp<StationDataComponent>(station, out var sd))
                comp.TargetStation = sd.UID;
        }

        if (string.IsNullOrWhiteSpace(title))
            title = Loc.GetString("requisitions-invoice-default-title");
        _metaData.SetEntityName(invoice, $"invoice ${cost} {title}");
        _hands.PickupOrDrop(actor, invoice);
    }

    private string SheetLabelServer(string matId, int rawAmount)
    {
        if (!Proto.TryIndex<MaterialPrototype>(matId, out var m))
            return $"{matId} {rawAmount}";

        var volume = _materialStorage.GetSheetVolume(m);
        if (volume <= 0)
            volume = 1;

        // Amount first, e.g. "6 Steel sheets".
        return $"{MathF.Round(rawAmount / (float) volume, 2)} {Loc.GetString(m.Name)} {Loc.GetString(m.Unit)}";
    }

    /// <summary>
    /// Queues up to <paramref name="qty"/> units of the recipe, one at a time, across the linked lathes that can
    /// print it — so an order spreads over machines and a machine that runs out of materials falls through to
    /// another. Returns how many units were <b>actually</b> queued (a machine can accept fewer than requested),
    /// and accumulates the customer's contribution that was applied into <paramref name="coverUsed"/>. Only the
    /// queued units get billed, so a unit that can't be produced never reaches the invoice.
    /// </summary>
    private int TryDispatchLathe(Entity<RequisitionsConsoleComponent> ent, LatheRecipePrototype recipe, int qty,
        Dictionary<string, int> perUnit, Dictionary<string, int> pool, Dictionary<string, int> coverUsed,
        out string? reason, EntityUid? flatpacker = null)
    {
        reason = null;
        var queued = 0;
        var hadCandidate = false;

        for (var u = 0; u < qty; u++)
        {
            // What the customer's remaining contribution covers for this single unit.
            var unitCover = new Dictionary<string, int>();
            foreach (var (mat, need) in perUnit)
            {
                var c = Math.Min(pool.GetValueOrDefault(mat), need);
                if (c > 0)
                    unitCover[mat] = c;
            }

            var placed = false;
            foreach (var machine in ent.Comp.LinkedMachines)
            {
                if (!_latheQuery.TryComp(machine, out var lathe))
                    continue;

                if (!_lathe.GetAvailableRecipes(machine, lathe).ContainsKey(recipe.ID))
                    continue;

                hadCandidate = true;
                MoveCover(ent.Owner, machine, unitCover, into: true);

                if (_lathe.TryAddToQueue(machine, recipe, 1))
                {
                    // Start staggered rather than immediately: starting many machines on the same tick spikes
                    // their combined power draw and browns out the APC. See ScheduleStart / Update.
                    ScheduleStart(machine);

                    // Record a job per printed unit so the console tracks progress and (for flatpack orders)
                    // routes each finished board to a flatpacker.
                    var jobs = EnsureComp<RequisitionsLatheJobComponent>(machine);
                    jobs.Jobs.Add(new RequisitionJob
                    {
                        Recipe = recipe.ID,
                        Console = ent.Owner,
                        Cover = unitCover.Count > 0 ? new Dictionary<string, int>(unitCover) : null,
                        Flatpacker = flatpacker,
                    });

                    // Spend this unit's contribution and remember it for billing.
                    foreach (var (mat, c) in unitCover)
                    {
                        pool[mat] = pool.GetValueOrDefault(mat) - c;
                        coverUsed[mat] = coverUsed.GetValueOrDefault(mat) + c;
                    }

                    ent.Comp.OutstandingJobs++; // locks the console until this print finishes
                    queued++;
                    placed = true;
                    break;
                }

                MoveCover(ent.Owner, machine, unitCover, into: false); // revert and try the next machine
            }

            if (!placed)
                break; // no linked machine could take another unit — stop here
        }

        if (queued > 0)
            UpdateUi(ent);
        else
            reason = Loc.GetString(hadCandidate ? "requisitions-fail-no-materials" : "requisitions-fail-no-machine");

        return queued;
    }

    /// <summary>
    /// Prints boards on a lathe like any other item; a transfer job on the lathe moves each finished board into a
    /// flatpacker once it's done printing. See <see cref="OnLatheItemProduced"/>. Returns the number of units
    /// actually queued.
    /// </summary>
    private int TryDispatchFlatpack(Entity<RequisitionsConsoleComponent> ent, LatheRecipePrototype recipe, int qty,
        Dictionary<string, int> perUnit, Dictionary<string, int> pool, Dictionary<string, int> coverUsed, out string? reason)
    {
        if (recipe.Result is null)
        {
            reason = Loc.GetString("requisitions-fail-no-machine");
            return 0;
        }

        EntityUid? flatpacker = null;
        foreach (var machine in ent.Comp.LinkedMachines)
        {
            if (HasComp<FlatpackCreatorComponent>(machine))
            {
                flatpacker = machine;
                break;
            }
        }

        if (flatpacker == null)
        {
            reason = Loc.GetString("requisitions-fail-no-flatpacker");
            return 0;
        }

        return TryDispatchLathe(ent, recipe, qty, perUnit, pool, coverUsed, out reason, flatpacker);
    }

    /// <summary>Moves the covered contribution between the console and a target machine (into it, or back out).</summary>
    private void MoveCover(EntityUid console, EntityUid machine, Dictionary<string, int> cover, bool into)
    {
        foreach (var (mat, c) in cover)
        {
            if (c <= 0)
                continue;

            var sign = into ? 1 : -1;
            _materialStorage.TryChangeMaterialAmount(console, mat, sign * -c, localOnly: true);
            _materialStorage.TryChangeMaterialAmount(machine, mat, sign * c);
        }
    }

    #endregion

    #region Flatpack transfer

    /// <summary>
    /// A requisition item finished printing: mark one job done. Flatpack boards are moved into the console's
    /// internal storage (from where they're fed to a flatpacker); everything else is delivered at the lathe.
    /// </summary>
    private void OnLatheItemProduced(Entity<RequisitionsLatheJobComponent> ent, ref LatheItemProducedEvent args)
    {
        var recipeId = args.Recipe.ID;
        var result = args.Result;

        var idx = ent.Comp.Jobs.FindIndex(j => j.Recipe == recipeId);
        if (idx < 0)
            return;

        var job = ent.Comp.Jobs[idx];
        ent.Comp.Jobs.RemoveAt(idx);
        if (ent.Comp.Jobs.Count == 0)
            RemCompDeferred<RequisitionsLatheJobComponent>(ent);

        if (!TryComp<RequisitionsConsoleComponent>(job.Console, out var console))
            return; // console gone: a non-flatpack item just stays at the lathe

        // This print no longer counts as in-progress.
        console.OutstandingJobs = Math.Max(0, console.OutstandingJobs - 1);

        // Flatpack items: move the actual printed board into the console's internal storage and try to feed a
        // flatpacker from it. Storing the real board (rather than a proto in memory) means nothing is lost if
        // packing stalls or the round restarts.
        if (job.Flatpacker != null && !TerminatingOrDeleted(result))
        {
            var container = _container.EnsureContainer<Container>(job.Console, console.FlatpackStorageId);
            if (_container.Insert(result, container))
            {
                console.NextFlatpackTry = TimeSpan.Zero;
                TryFeedFlatpackers((job.Console, console));
            }
        }

        UpdateUi((job.Console, console));
    }

    /// <summary>
    /// The lathe lost power and aborts its in-progress print, which our per-item dispatch does not resume. Return
    /// each outstanding job's contributed materials to the customer and clear the console's in-progress count.
    /// </summary>
    private void OnJobLathePowerChanged(Entity<RequisitionsLatheJobComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        ReleaseJobs(ent);
        ent.Comp.Jobs.Clear();
        RemCompDeferred<RequisitionsLatheJobComponent>(ent);
    }

    /// <summary>The lathe (and its jobs) are being deleted mid-print — release whatever's still outstanding.</summary>
    private void OnJobLatheShutdown(Entity<RequisitionsLatheJobComponent> ent, ref ComponentShutdown args)
    {
        ReleaseJobs(ent);
        ent.Comp.Jobs.Clear();
    }

    /// <summary>
    /// For every outstanding job on a lathe, returns the customer's contributed materials (the aborted lathe was
    /// handed them back) and clears the console's in-progress count so it doesn't stay locked.
    /// </summary>
    private void ReleaseJobs(Entity<RequisitionsLatheJobComponent> ent)
    {
        foreach (var job in ent.Comp.Jobs)
        {
            if (!TryComp<RequisitionsConsoleComponent>(job.Console, out var console))
                continue;

            if (job.Cover is { } cover)
            {
                var coords = Transform(job.Console).Coordinates;
                foreach (var (mat, amount) in cover)
                {
                    var take = Math.Min(amount, _materialStorage.GetMaterialAmount(ent.Owner, mat));
                    if (take <= 0)
                        continue;

                    _materialStorage.SpawnMultipleFromMaterial(take, mat, coords, out var overflow);
                    _materialStorage.TryChangeMaterialAmount(ent.Owner, mat, -(take - overflow));
                }
            }

            console.OutstandingJobs = Math.Max(0, console.OutstandingJobs - 1);
            UpdateUi((job.Console, console));
        }
    }

    /// <summary>
    /// Feeds one stored board from the console's internal storage into an idle linked flatpacker. No spawning or
    /// deleting: the real board is reparented into the flatpacker, or left in storage to retry later.
    /// </summary>
    private void TryFeedFlatpackers(Entity<RequisitionsConsoleComponent> console)
    {
        if (_timing.CurTime < console.Comp.NextFlatpackTry)
            return;

        if (!_container.TryGetContainer(console, console.Comp.FlatpackStorageId, out var container) || container.ContainedEntities.Count == 0)
            return;

        foreach (var machine in console.Comp.LinkedMachines)
        {
            if (!TryComp<FlatpackCreatorComponent>(machine, out var creator) || !_flatpack.IsIdle((machine, creator)))
                continue;

            var board = container.ContainedEntities[0];

            // TryPackBoard reparents the board into the flatpacker on success; on failure it drops it in the
            // world, so we pull it back into storage and back off.
            if (_flatpack.TryPackBoard((machine, creator), board))
                return;

            _container.Insert(board, container);
            console.Comp.NextFlatpackTry = _timing.CurTime + TimeSpan.FromSeconds(2);
            return;
        }

        // Nothing idle right now — check again shortly.
        console.Comp.NextFlatpackTry = _timing.CurTime + TimeSpan.FromSeconds(1);
    }

    private static readonly TimeSpan StartStagger = TimeSpan.FromSeconds(0.2);
    private readonly Queue<EntityUid> _pendingStarts = new();
    private readonly HashSet<EntityUid> _pendingStartSet = new();
    private TimeSpan _nextStart;

    /// <summary>Queues a lathe to be kicked into production, spaced out from other starts (see <see cref="Update"/>).</summary>
    private void ScheduleStart(EntityUid lathe)
    {
        if (_pendingStartSet.Add(lathe))
            _pendingStarts.Enqueue(lathe);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        // Start at most one queued lathe every StartStagger seconds so a big order that involvs different lathes
        // doesn't flip every machine into its high-power working state on the same tick and brown out the APC.
        if (_pendingStarts.Count > 0 && now >= _nextStart)
        {
            var lathe = _pendingStarts.Dequeue();
            _pendingStartSet.Remove(lathe);
            if (!TerminatingOrDeleted(lathe))
                _lathe.TryStartProducing(lathe);
            _nextStart = now + StartStagger;
        }

        var query = EntityQueryEnumerator<RequisitionsConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
            TryFeedFlatpackers((uid, comp));
    }

    /// <summary>Price of a raw material amount, charged per sheet.</summary>
    private int SheetCost(RequisitionsConsoleComponent comp, string materialId, int rawAmount)
    {
        if (rawAmount <= 0 || !Proto.TryIndex<MaterialPrototype>(materialId, out var mat))
            return 0;

        var volume = _materialStorage.GetSheetVolume(mat);
        if (volume <= 0)
            volume = 1;

        return (int) MathF.Round(rawAmount / (float) volume * GetPrice(comp, materialId));
    }

    /// <summary>The customer changed their mind — return the sheets they inserted toward this order.</summary>
    private void OnCancel(Entity<RequisitionsConsoleComponent> ent, ref RequisitionCancelMessage args)
    {
        _materialStorage.EjectAllMaterial(ent.Owner);
        UpdateUi(ent, args.Actor);
    }

    /// <summary>Ejects boards stuck in the internal flatpack storage back into the world (access-gated).</summary>
    private void OnEjectFlatpacks(Entity<RequisitionsConsoleComponent> ent, ref RequisitionEjectFlatpacksMessage args)
    {
        if (!HasConfigAccess(ent, args.Actor))
        {
            _popup.PopupEntity(Loc.GetString("requisitions-access-denied"), ent.Owner, args.Actor);
            return;
        }

        if (_container.TryGetContainer(ent, ent.Comp.FlatpackStorageId, out var container))
            _container.EmptyContainer(container);

        UpdateUi(ent);
    }

    #endregion

    #region UI state

    protected override void UpdateUi(Entity<RequisitionsConsoleComponent> ent, EntityUid? actor = null)
    {
        if (!_ui.IsUiOpen(ent.Owner, RequisitionsConsoleUiKey.Key))
            return;

        var state = new RequisitionsConsoleState
        {
            Catalogue = BuildCatalogue(ent),
            Stock = BuildStock(ent),
            Contributed = _materialStorage.GetStoredMaterials(ent.Owner, localOnly: true).ToDictionary(kv => kv.Key.Id, kv => kv.Value),
            MaterialPrices = ent.Comp.MaterialPrices.ToDictionary(kv => kv.Key.Id, kv => kv.Value),
            MaterialNames = BuildMaterialNames(ent),
            Fees = ent.Comp.Fees,
            FlatpackerLinked = ent.Comp.FlatpackerLinked,
            FlatpackMultiplier = ent.Comp.FlatpackMaterialMultiplier,
            // A shared BUI state can't be tailored per-viewer, so the config tab shows if any current viewer is
            // authorised. Every config action is still re-checked server-side regardless.
            HasConfigAccess = _ui.GetActors(ent.Owner, RequisitionsConsoleUiKey.Key).Any(a => HasConfigAccess(ent, a)),
            Processing = ent.Comp.OutstandingJobs > 0,
            PendingFlatpacks = _container.TryGetContainer(ent, ent.Comp.FlatpackStorageId, out var fpStore) ? fpStore.ContainedEntities.Count : 0,
            DetailedInvoice = ent.Comp.DetailedInvoice,
        };

        if (state.HasConfigAccess)
            state.Linkable = BuildLinkable(ent);

        _ui.SetUiState(ent.Owner, RequisitionsConsoleUiKey.Key, state);
    }

    private List<RequisitionCatalogueEntry> BuildCatalogue(Entity<RequisitionsConsoleComponent> ent)
    {
        // Squash by an identity of "same result + same materials" so genuinely identical items (e.g. four
        // aprons offered by different recipes/machines) collapse to one line, while variants that cost
        // differently stay separate.
        var merged = new Dictionary<string, RequisitionCatalogueEntry>();

        foreach (var machine in ent.Comp.LinkedMachines)
        {
            if (!_latheQuery.TryComp(machine, out var lathe))
                continue;

            foreach (var (recipeId, count) in _lathe.GetAvailableRecipes(machine, lathe))
            {
                if (!Proto.TryIndex(recipeId, out var recipe))
                    continue;

                var materials = recipe.Materials.OrderBy(kv => kv.Key.Id).Select(kv => $"{kv.Key.Id}:{kv.Value}");
                var signature = $"{recipe.Result?.Id}|{string.Join(",", materials)}";

                // A negative count is the "unlimited" sentinel for static recipes; a non-negative one is the
                // remaining research prints.
                var unlimited = count < 0;

                if (!merged.TryGetValue(signature, out var entry))
                {
                    entry = new RequisitionCatalogueEntry
                    {
                        RecipeId = recipeId,
                        Name = Lathe.GetRecipeName(recipe),
                        Result = recipe.Result?.Id,
                        Materials = recipe.Materials.ToDictionary(kv => kv.Key.Id, kv => kv.Value),
                        Flatpackable = ent.Comp.FlatpackerLinked && IsFlatpackable(recipe),
                        PrintsRemaining = unlimited ? null : count,
                    };
                    merged[signature] = entry;
                }
                else if (unlimited)
                {
                    entry.PrintsRemaining = null; // available unlimited somewhere → not limited
                }
                else if (entry.PrintsRemaining is { } current)
                {
                    entry.PrintsRemaining = Math.Max(current, count); // most prints any linked source can do
                }

                entry.SourceCount++;
            }
        }

        return merged.Values.OrderBy(e => e.Name).ToList();
    }

    private bool IsFlatpackable(LatheRecipePrototype recipe)
    {
        // The flatpacker packs machine/computer boards; a recipe is flatpackable if its result is one.
        if (recipe.Result is not { } result || !Proto.TryIndex<EntityPrototype>(result, out var proto))
            return false;

        return proto.Components.ContainsKey("MachineBoard") || proto.Components.ContainsKey("ComputerBoard");
    }

    private Dictionary<string, int> BuildStock(Entity<RequisitionsConsoleComponent> ent)
    {
        // Only report the stock of linked ore silos. Summing individual lathes is misleading (one loaded lathe
        // among ten would read as department-wide stock), so with no silo linked the stock panel is simply empty.
        var stock = new Dictionary<string, int>();
        foreach (var machine in ent.Comp.LinkedMachines)
        {
            if (!HasComp<OreSiloComponent>(machine))
                continue;

            foreach (var (mat, amount) in _materialStorage.GetStoredMaterials(machine, localOnly: true))
                stock[mat.Id] = stock.GetValueOrDefault(mat.Id) + amount;
        }

        return stock;
    }

    private Dictionary<string, string> BuildMaterialNames(Entity<RequisitionsConsoleComponent> ent)
    {
        var names = new Dictionary<string, string>();
        foreach (var mat in ent.Comp.MaterialPrices.Keys)
        {
            if (Proto.TryIndex(mat, out var proto))
                names[mat.Id] = Loc.GetString(proto.Name);
        }

        return names;
    }

    private List<RequisitionLinkEntry> BuildLinkable(Entity<RequisitionsConsoleComponent> ent)
    {
        var result = new List<RequisitionLinkEntry>();
        var seen = new HashSet<EntityUid>();

        var coords = Transform(ent).Coordinates;

        void Add(EntityUid machine)
        {
            if (!seen.Add(machine))
                return;

            result.Add(new RequisitionLinkEntry
            {
                Machine = GetNetEntity(machine),
                Label = MetaData(machine).EntityName,
                Linked = ent.Comp.LinkedMachines.Contains(machine),
                InRange = CanLink(ent.Owner, machine),
                Flatpacker = HasComp<FlatpackCreatorComponent>(machine),
            });
        }

        var nearbyLathes = new HashSet<Entity<LatheComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, nearbyLathes);
        foreach (var machine in nearbyLathes)
            Add(machine);

        var nearbyFlatpackers = new HashSet<Entity<FlatpackCreatorComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, nearbyFlatpackers);
        foreach (var machine in nearbyFlatpackers)
            Add(machine);

        var nearbySilos = new HashSet<Entity<OreSiloComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, nearbySilos);
        foreach (var machine in nearbySilos)
            Add(machine);

        // Include already-linked machines even if they've drifted out of range.
        foreach (var machine in ent.Comp.LinkedMachines)
            Add(machine);

        return result;
    }

    #endregion

    private IEnumerable<RequisitionFee> FeesFor(RequisitionsConsoleComponent comp, string recipeId, bool flatpack)
    {
        foreach (var fee in comp.Fees)
        {
            switch (fee.Scope)
            {
                case RequisitionFeeScope.Flatpack when flatpack:
                case RequisitionFeeScope.All:
                    yield return fee;
                    break;
                case RequisitionFeeScope.Specific when fee.Recipes.Contains(recipeId):
                    yield return fee;
                    break;
            }
        }
    }

    private int GetPrice(RequisitionsConsoleComponent comp, string material)
    {
        return comp.MaterialPrices.TryGetValue(material, out var price) ? price : comp.FallbackMaterialPrice;
    }
}
