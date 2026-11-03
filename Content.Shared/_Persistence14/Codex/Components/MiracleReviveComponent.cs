using Content.Shared.Damage;
using Robust.Shared.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Codex.Components;

[RegisterComponent]
public sealed partial class MiracleReviveComponent : Component
{
    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier ZapHeal = default!;

    /// <summary>
    /// The electrical damage from getting zapped.
    /// </summary>
    [DataField]
    public int ZapDamage = 5;

    /// <summary>
    /// How long the victim will be electrocuted after getting zapped.
    /// </summary>
    [DataField]
    public TimeSpan WritheDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The sound to play when someone is zapped.
    /// </summary>
    [DataField]
    public SoundSpecifier? ZapSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    /// <summary>
    /// The sound to play when starting the doafter.
    /// </summary>
    [DataField]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    [DataField]
    public SoundSpecifier? FailureSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");

    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [DataField]
    public SoundSpecifier? ReadySound = new SoundPathSpecifier("/Audio/Items/Defib/defib_ready.ogg");
}
