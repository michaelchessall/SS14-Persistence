using Content.Server.Radiation.Components;
using Content.Shared._Persistence14.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Events;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._Persistence14.Botany;

/// <inheritdoc/>
public sealed partial class PlantRadiotrophySystem : EntitySystem
{

    [Dependency] private PlantTraySystem _tray = default!;

    private const string RadiationNutrientId = "Radiation";
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

    }

    [SubscribeLocalEvent]
    private void OnTrayUpdate(Entity<PlantTrayComponent> ent, ref TrayUpdateEvent args)
    {
        if (!TryComp(ent.Owner, out RadiationReceiverComponent? receiver))
            return;
        if (!_tray.TryGetAlivePlant(ent.AsNullable(), out var plant))
            return;

        var currentTrayRadiation = _tray.GetNutrient(ent.AsNullable(), RadiationNutrientId);
        var radiationMax = (receiver.CurrentRadiation * 10 - currentTrayRadiation); // I love hardcoded magic numbers :godo:

        var radiationToAdd = FixedPoint2.New(receiver.CurrentRadiation);
        if (radiationToAdd > radiationMax)
            radiationToAdd = radiationMax;

        if (radiationToAdd > FixedPoint2.Zero)
            _tray.AdjustNutrient(ent.AsNullable(), radiationToAdd, RadiationNutrientId);
    }
}
