using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Kitchen;

public sealed partial class RecipeManager : EntitySystem
{
    public Dictionary<EntProtoId, List<FoodRecipePrototype>> Recipes { get; private set; } = new(); // Persi14: Type changed for cooking rework

    public override void Initialize()
    {
        base.Initialize();

        ReloadRecipes();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<FoodRecipePrototype>())
            ReloadRecipes();
    }

    private void ReloadRecipes()
    {
        Recipes = ProtoMan
            .EnumeratePrototypes<FoodRecipePrototype>()
            .GroupBy(i => i.CookingDevice)
            .ToDictionary( // Persi14: Convert to new type
                group => group.Key,
                group => group.Where(x => !x.SecretRecipe)
                    .OrderByDescending(x => x.IngredientCount())
                    .ToList()
            );
    }
}
