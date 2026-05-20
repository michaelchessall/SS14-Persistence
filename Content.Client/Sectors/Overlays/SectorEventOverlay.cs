using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Sectors.Overlays;

public sealed class SectorEventOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderProto = "SectorEventTint";

    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly ShaderInstance _shader;

    public Color TintColor = Color.Transparent;
    public float TintNoiseStrength;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public SectorEventOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(ShaderProto).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (TintColor.A <= 0f)
            return false;

        if (!_entities.TryGetComponent(_players.LocalEntity, out EyeComponent? eyeComp))
            return false;

        return args.Viewport.Eye == eyeComp.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tintColor", new Vector4(TintColor.R, TintColor.G, TintColor.B, TintColor.A));
        _shader.SetParameter("noiseStrength", Math.Clamp(TintNoiseStrength, 0f, 1f));

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
