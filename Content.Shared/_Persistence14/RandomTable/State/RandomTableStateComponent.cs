using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.RandomTable.State;

/// <summary>
/// A component used to store any data that is needed from a RandomDataTable.
/// Data dictionary allows serializable storage of arbitrary data.
/// </summary>
[RegisterComponent]
public sealed partial class RandomTableStateComponent : Component
{
    [DataField]
    public EntityUid? User { get; set; }

    [DataField]
    public EntityUid? Source { get; set; }

    [DataField]
    public EntityUid? Target { get; set; }

    [DataField]
    public Dictionary<string, RandomTableContextData> Data = new();

    /// <summary>
    /// Returns whether a given set of context elements are present within the table state.
    /// </summary>
    public bool EnsureContext(RandomTableContextElement elements, params string[] dataKeys)
    {
        if ((elements & RandomTableContextElement.User) != 0 && User is null) return false;
        if ((elements & RandomTableContextElement.Source) != 0 && Source is null) return false;
        if ((elements & RandomTableContextElement.Target) != 0 && Target is null) return false;

        if ((elements & RandomTableContextElement.Data) != 0)
            foreach (var dataKey in dataKeys)
                if (!Data.TryGetValue(dataKey, out _))
                    return false;

        return true;
    }
}

[Flags]
public enum RandomTableContextElement : byte
{
    None = 0,
    User = 1 << 0,
    Source = 1 << 1,
    Target = 1 << 2,
    Data = 1 << 3
}

/// <summary>
/// A flexible serializable data type allowing arbitrary data storage within a RandomTableStateComponent.
/// </summary>
[DataDefinition]
public sealed partial class RandomTableContextData
{
    [DataField("int")]
    public int? Int { get; set; } = null;

    [DataField("float")]
    public float? Float { get; set; } = null;

    [DataField("string")]
    public string? String { get; set; } = null;

    [DataField("bool")]
    public bool? Bool { get; set; } = null;

    [DataField("prototype")]
    public string? ProtoId { get; set; } = null;

    public bool TryGetInt([NotNullWhen(true)] out int? val)
    {
        val = Int ?? (int?)Float;
        return val is not null;
    }

    public bool TryGetFloat([NotNullWhen(true)] out float? val)
    {
        val = Float ?? (float?)Int;
        return val is not null;
    }

    public bool TryGetString([NotNullWhen(true)] out string? val)
    {
        val = String;
        return val is not null;
    }

    public bool TryGetBool([NotNullWhen(true)] out bool? val)
    {
        val = Bool;
        return val is not null;
    }

    public bool TryGetPrototype<T>(IPrototypeManager protoMan, [NotNullWhen(true)] out T? proto) where T : class, IPrototype
    {
        proto = null;
        if (ProtoId is null) return false;
        if (protoMan.TryIndex(ProtoId, out proto))
            return true;
        return false;
    }

    public static implicit operator int(RandomTableContextData data) => data.Int ?? (int?)data.Float ?? 0;
    public static implicit operator RandomTableContextData(int val) => new RandomTableContextData() { Int = val };

    public static implicit operator float(RandomTableContextData data) => data.Float ?? (float?)data.Int ?? 0;
    public static implicit operator RandomTableContextData(float val) => new RandomTableContextData() { Float = val };

    public static implicit operator string(RandomTableContextData data) => data.String ?? "";
    public static implicit operator RandomTableContextData(string val) => new RandomTableContextData() { String = val };

    public static implicit operator bool(RandomTableContextData data) => data.Bool ?? false;
    public static implicit operator RandomTableContextData(bool val) => new RandomTableContextData() { Bool = val };
}