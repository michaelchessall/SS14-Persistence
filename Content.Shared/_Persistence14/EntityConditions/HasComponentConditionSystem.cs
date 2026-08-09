using Content.Shared.EntityConditions;

namespace Content.Shared._Persistence14.EntityConditions;

public sealed partial class HasComponentConditionSystem : EntityConditionSystem<TransformComponent, HasComponent>
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    /// <inheritdoc/>
    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<HasComponent> args)
    {
        var registration = _factory.GetRegistration(args.Condition.Component);

        args.Result = HasComp(entity.Owner, registration.Type);
    }
}