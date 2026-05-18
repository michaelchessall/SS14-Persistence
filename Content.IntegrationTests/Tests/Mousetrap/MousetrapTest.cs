using Content.IntegrationTests.Tests.Movement;
using Content.Server.NPC.HTN;
using Content.Shared.Damage.Systems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mousetrap;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Mousetrap;

/// <summary>
/// Spawns a mouse and a mousetrap.
/// Makes the mouse cross the inactive mousetrap, then activates the trap and
/// makes the mouse try to cross back over it.
/// </summary>
/// <remarks>
/// Yep, every time the tests run, a virtual mouse dies. Sorry.
/// </remarks>
public sealed class MousetrapMouseMoveOverTest : MovementTest
{
    private static readonly EntProtoId MousetrapProtoId = "Mousetrap";
    private static readonly EntProtoId MouseProtoId = "MobMouse";
    protected override string PlayerPrototype => MouseProtoId.Id; // use a mouse as the player entity
}

/// <summary>
/// Spawns a mousetrap and makes the player walk over it without shoes.
/// Gives the player some shoes and makes them walk back over the trap.
/// </summary>
public sealed class MousetrapHumanMoveOverTest : MovementTest
{
    private static readonly EntProtoId MousetrapProtoId = "Mousetrap";
    private const string ShoesProtoId = "InteractionTestShoes";

    [TestPrototypes]
    private static readonly string TestPrototypes = $@"
    - type: entity
      parent: ClothingShoesBase
      id: {ShoesProtoId}
      components:
      - type: Sprite
        sprite: Clothing/Shoes/Boots/workboots.rsi
    ";
}
