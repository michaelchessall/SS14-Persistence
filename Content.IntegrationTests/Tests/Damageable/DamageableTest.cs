using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Damageable
{
    [TestFixture]
    [TestOf(typeof(DamageableComponent))]
    [TestOf(typeof(DamageableSystem))]
    public sealed class DamageableTest
    {
        private const string TestDamageableEntityId = "TestDamageableEntityId";
        private const string TestGroup1 = "TestGroup1";
        private const string TestGroup2 = "TestGroup2";
        private const string TestGroup3 = "TestGroup3";
        private const string TestDamage1 = "TestDamage1";
        private const string TestDamage2a = "TestDamage2a";
        private const string TestDamage2b = "TestDamage2b";

        private const string TestDamage3a = "TestDamage3a";

        private const string TestDamage3b = "TestDamage3b";
        private const string TestDamage3c = "TestDamage3c";

        [TestPrototypes]
        private const string Prototypes = $@"
# Define some damage groups
- type: damageType
  id: {TestDamage1}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage2a}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage2b}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage3a}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage3b}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage3c}
  name: damage-type-blunt

# Define damage Groups with 1,2,3 damage types
- type: damageGroup
  id: {TestGroup1}
  name: damage-group-brute
  damageTypes:
    - {TestDamage1}

- type: damageGroup
  id: {TestGroup2}
  name: damage-group-brute
  damageTypes:
    - {TestDamage2a}
    - {TestDamage2b}

- type: damageGroup
  id: {TestGroup3}
  name: damage-group-brute
  damageTypes:
    - {TestDamage3a}
    - {TestDamage3b}
    - {TestDamage3c}

# This container should not support TestDamage1 or TestDamage2b
- type: damageContainer
  id: testDamageContainer
  supportedGroups:
    - {TestGroup3}
  supportedTypes:
    - {TestDamage2a}

- type: entity
  id: {TestDamageableEntityId}
  name: {TestDamageableEntityId}
  components:
  - type: Damageable
    damageContainer: testDamageContainer
";
    }
}
