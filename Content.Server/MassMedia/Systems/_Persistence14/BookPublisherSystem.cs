using Content.Server.Discord;
using Content.Server.Discord._Persistence14;
using Content.Server.MassMedia.Components._Persistence14;
using Content.Server.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.IdentityManagement;
using Content.Shared.MassMedia.Components._Persistence14;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.MassMedia.Systems._Persistence14;

public sealed class BookPublisherSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private WebhookIdentifier? _webhookId;
    private Robust.Shared.Maths.Color _webhookEmbedColor;
    private FontFamily? _fontFamily;
    private Font? _bodyFont;
    private Font? _italicFont;
    private Font? _titleFont;
    private Font? _titleFontItalic;
    private Font? _smallFont;
    private Dictionary<(float size, bool bold, bool italic), Font> _fontCache = new();
    private const float SS14_DEFAULT_SIZE = 16f;
    private static readonly Regex SS14TagRegex = new Regex(@"\[([/]?)([a-zA-Z]+)(?:=([^\]]*))?\]", RegexOptions.Compiled);

    public override void Initialize()
    {
        base.Initialize();

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var resourcesDir = Path.Combine(baseDir, "..", "..", "Resources", "Fonts");
            resourcesDir = Path.GetFullPath(resourcesDir);

            if (Directory.Exists(resourcesDir))
            {
                var notoRegular = Path.Combine(resourcesDir, "NotoSans", "NotoSans-Regular.ttf");
                var notoBold = Path.Combine(resourcesDir, "NotoSans", "NotoSans-Bold.ttf");
                var notoItalic = Path.Combine(resourcesDir, "NotoSans", "NotoSans-Italic.ttf");
                var notoItalicBold = Path.Combine(resourcesDir, "NotoSans", "NotoSans-BoldItalic.ttf");

                var collection = new FontCollection();
                if (File.Exists(notoRegular))
                {
                    collection.Add(notoRegular);
                }
                if (File.Exists(notoBold))
                {
                    collection.Add(notoBold);
                }
                if (File.Exists(notoItalic))
                {
                    collection.Add(notoItalic);
                }
                if (File.Exists(notoItalicBold))
                {
                    collection.Add(notoItalicBold);
                }

                foreach (var family in collection.Families)
                {
                    if (family.Name.Contains("Noto Sans", StringComparison.OrdinalIgnoreCase))
                    {
                        _fontFamily = family;
                        break;
                    }
                }

                if (_fontFamily != null)
                {
                    _bodyFont = new Font(_fontFamily.Value, SS14_DEFAULT_SIZE, FontStyle.Regular);
                    _italicFont = new Font(_fontFamily.Value, SS14_DEFAULT_SIZE, FontStyle.Italic);
                    _titleFont = new Font(_fontFamily.Value, GetHeaderSize(1), FontStyle.Bold);
                    _titleFontItalic = new Font(_fontFamily.Value, GetHeaderSize(1), FontStyle.Bold | FontStyle.Italic);
                    _smallFont = new Font(_fontFamily.Value, 10f, FontStyle.Regular);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not load fonts for book publisher: {ex}");
        }

        _cfg.OnValueChanged(CCVars.DiscordNewsWebhook, value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
                _discord.GetWebhook(value, data => _webhookId = data.ToIdentifier());
        }, true);

        _cfg.OnValueChanged(CCVars.DiscordNewsWebhookEmbedColor, value =>
        {
            _webhookEmbedColor = Robust.Shared.Maths.Color.LawnGreen;
            if (Robust.Shared.Maths.Color.TryParse(value, out var color))
                _webhookEmbedColor = color;
        }, true);

        SubscribeLocalEvent<BookPublisherComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BookPublisherComponent, EntInsertedIntoContainerMessage>(OnSlotInserted);
        SubscribeLocalEvent<BookPublisherComponent, EntRemovedFromContainerMessage>(OnSlotRemoved);

        Subs.BuiEvents<BookPublisherComponent>(BookPublisherUiKey.Key, subs =>
        {
            subs.Event<BookPublisherPublishMessage>(OnPublish);
            subs.Event<BookPublisherEjectMessage>(OnEject);
            subs.Event<BookPublisherRefreshMessage>(OnRefresh);
        });
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BookPublisherComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PublishEnabled || _timing.CurTime < comp.NextPublish)
                continue;
            comp.PublishEnabled = true;
            UpdateUi((uid, comp));
        }
    }

    private void OnMapInit(Entity<BookPublisherComponent> ent, ref MapInitEvent args)
    {
        _itemSlots.AddItemSlot(ent.Owner, "Book", ent.Comp.BookSlot);
    }

    private void OnSlotInserted(Entity<BookPublisherComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != "Book")
            return;
        UpdateUi(ent);
    }

    private void OnSlotRemoved(Entity<BookPublisherComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != "Book")
            return;
        UpdateUi(ent);
    }

    private void OnPublish(Entity<BookPublisherComponent> ent, ref BookPublisherPublishMessage msg)
    {
        if (!ent.Comp.PublishEnabled || !CanUse(msg.Actor, ent.Owner))
            return;

        var bookSlot = ent.Comp.BookSlot;
        if (bookSlot.Item == null)
        {
            _popup.PopupEntity(Loc.GetString("book-publisher-no-book"), ent.Owner, msg.Actor);
            return;
        }

        if (!TryComp<PaperComponent>(bookSlot.Item.Value, out var paper))
        {
            _popup.PopupEntity(Loc.GetString("book-publisher-no-paper"), ent.Owner, msg.Actor);
            return;
        }

        ent.Comp.PublishEnabled = false;
        ent.Comp.NextPublish = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.PublishCooldown);

        var title = msg.Title.Trim();
        var content = paper.Content;

        _audio.PlayPvs(ent.Comp.ConfirmSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("book-publisher-publishing", ("title", title)), ent.Owner, msg.Actor);

        PublishBook(ent, title, content, msg.Actor);

        UpdateUi(ent);
    }

    private async void PublishBook(Entity<BookPublisherComponent> ent, string title, string content, EntityUid? actor)
    {
        var files = new List<WebhookFile>();
        if (!string.IsNullOrEmpty(content))
        {
            var imageBytes = RenderSS14Markup(content);
            files.Add(new WebhookFile("book.png", imageBytes));
            Log.Info($"Rendered book, size: {imageBytes.Length} bytes");
        }

        if (_webhookId != null && actor != null)
        {
            var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, actor.Value);
            RaiseLocalEvent(tryGetIdentityShortInfoEvent);
            string? authorName = tryGetIdentityShortInfoEvent.Title;

            var payload = new WebhookPayload
            {
                Content = $"**{title}** published by {authorName}"
            };

            try
            {
                await _discord.CreateMessageWithFiles(_webhookId.Value, payload, files);
            }
            catch (Exception ex)
            {
                Log.Error($"Book publish failed: {ex}");
            }
        }

        if (actor != null)
            Log.Info($"Book '{title}' published by {ToPrettyString(actor.Value)}");
            _adminLogger.Add(
            LogType.Chat,
            LogImpact.Medium,
            $"{ToPrettyString(actor):actor} published book {title}");
    }

    private sealed class TextSegment
    {
        public string Text;
        public float FontSize;
        public bool Bold;
        public bool Italic;
        public SixLabors.ImageSharp.Color Color;

        public TextSegment(string text, float fontSize, bool bold, bool italic, SixLabors.ImageSharp.Color color)
        {
            Text = text;
            FontSize = fontSize;
            Bold = bold;
            Italic = italic;
            Color = color;
        }
    }

    private List<TextSegment> ParseSS14Markup(string content)
    {
        var segments = new List<TextSegment>();
        float currentSize = SS14_DEFAULT_SIZE;
        bool currentBold = false;
        bool currentItalic = false;
        SixLabors.ImageSharp.Color currentColor = SixLabors.ImageSharp.Color.Black;

        var stack = new Stack<(string tag, float size, bool bold, bool italic, SixLabors.ImageSharp.Color color)>();
        var lastIndex = 0;
        var match = SS14TagRegex.Match(content);

        while (match.Success)
        {
            if (match.Index > lastIndex)
            {
                var text = content.Substring(lastIndex, match.Index - lastIndex);
                segments.Add(new TextSegment(text, currentSize, currentBold, currentItalic, currentColor));
            }

            var isClosing = match.Groups[1].Value == "/";
            var tagName = match.Groups[2].Value.ToLower();
            var param = match.Groups[3].Value;

            if (isClosing)
            {
                while (stack.Count > 0)
                {
                    var item = stack.Pop();
                    if (item.tag == tagName)
                        break;
                }

                if (stack.Count > 0)
                {
                    var top = stack.Peek();
                    currentSize = top.size;
                    currentBold = top.bold;
                    currentItalic = top.italic;
                    currentColor = top.color;
                }
                else
                {
                    currentSize = SS14_DEFAULT_SIZE;
                    currentBold = false;
                    currentItalic = false;
                    currentColor = SixLabors.ImageSharp.Color.Black;
                }
            }
            else
            {
                float newSize = currentSize;
                bool newBold = currentBold;
                bool newItalic = currentItalic;
                SixLabors.ImageSharp.Color newColor = currentColor;

                if (tagName == "bold")
                {
                    newBold = true;
                }
                else if (tagName == "italic")
                {
                    newItalic = true;
                }
                else if (tagName == "head")
                {
                    int level = 1;
                    if (!string.IsNullOrEmpty(param) && int.TryParse(param, out level))
                    {
                        newSize = GetHeaderSize(level);
                        newBold = level <= 2;
                    }
                }
                else if (tagName == "color")
                {
                    if (TryParseColor(param, out var color))
                    {
                        newColor = color;
                    }
                }

                stack.Push((tagName, newSize, newBold, newItalic, newColor));
                currentSize = newSize;
                currentBold = newBold;
                currentItalic = newItalic;
                currentColor = newColor;
            }

            lastIndex = match.Index + match.Length;
            match = match.NextMatch();
        }

        if (lastIndex < content.Length)
        {
            var text = content.Substring(lastIndex);
            segments.Add(new TextSegment(text, currentSize, currentBold, currentItalic, currentColor));
        }

        return segments;
    }

    private float GetHeaderSize(int level)
    {
        return (float)Math.Ceiling(SS14_DEFAULT_SIZE * 2.0 / Math.Sqrt(level));
    }

    private bool TryParseColor(string param, out SixLabors.ImageSharp.Color color)
    {
        color = SixLabors.ImageSharp.Color.Black;

        if (Robust.Shared.Maths.Color.TryParse(param, out var robustColor))
        {
            color = SixLabors.ImageSharp.Color.FromRgb(
                (byte)(robustColor.R * 255),
                (byte)(robustColor.G * 255),
                (byte)(robustColor.B * 255));
            return true;
        }

        try
        {
            color = SixLabors.ImageSharp.Color.Parse(param);
            return true;
        }
        catch { }

        return false;
    }

    private sealed class LineLayout
    {
        public List<(string text, float fontSize, bool bold, bool italic, SixLabors.ImageSharp.Color color, float x)> Words;
        public float LineHeight;
        public float Baseline;

        public LineLayout()
        {
            Words = new List<(string, float, bool, bool, SixLabors.ImageSharp.Color, float)>();
        }
    }

    private byte[] RenderSS14Markup(string content)
    {
        const int width = 750;
        const int margin = 40;
        const int minHeight = 1050;

        var segments = ParseSS14Markup(content);

        var words = new List<(string text, float fontSize, bool bold, bool italic, SixLabors.ImageSharp.Color color)>();
        foreach (var seg in segments)
        {
            var wordsInSeg = seg.Text.Split(new[] { ' ', '\t' }, StringSplitOptions.None);
            bool firstWord = true;
            foreach (var word in wordsInSeg)
            {
                if (word == "")
                {
                    if (!firstWord)
                    {
                        words.Add((" ", seg.FontSize, seg.Bold, seg.Italic, seg.Color));
                    }
                    continue;
                }

                if (!firstWord)
                {
                    words.Add((" ", seg.FontSize, seg.Bold, seg.Italic, seg.Color));
                }
                firstWord = false;

                var lines = word.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > 0)
                    {
                        words.Add(("\n", seg.FontSize, seg.Bold, seg.Italic, seg.Color));
                    }
                    if (lines[i].Length > 0)
                    {
                        words.Add((lines[i], seg.FontSize, seg.Bold, seg.Italic, seg.Color));
                    }
                }
            }
        }

        var layoutLines = new List<LineLayout>();
        float y = margin;
        int wordIndex = 0;

        while (wordIndex < words.Count)
        {
            var line = new LineLayout();
            float x = margin;
            float lineMaxFontSize = 0f;
            bool lineHasContent = false;

            while (wordIndex < words.Count)
            {
                var (text, fontSize, bold, italic, color) = words[wordIndex];

                if (text == "\n")
                {
                    wordIndex++;
                    if (!lineHasContent)
                    {
                        line.LineHeight = SS14_DEFAULT_SIZE * 1.2f;
                        layoutLines.Add(line);
                        y += line.LineHeight;
                    }
                    break;
                }

                var font = GetFont(fontSize, bold, italic);
                var textOptions = new TextOptions(font);
                var size = TextMeasurer.MeasureSize(text, textOptions);

                if (x + size.Width > width - margin)
                {
                    if (!lineHasContent)
                    {
                        line.Words.Add((text, fontSize, bold, italic, color, x));
                        lineHasContent = true;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    line.Words.Add((text, fontSize, bold, italic, color, x));
                    x += size.Width;
                    lineHasContent = true;
                }

                if (fontSize > lineMaxFontSize)
                    lineMaxFontSize = fontSize;
                wordIndex++;
            }

            if (line.Words.Count > 0)
            {
                line.LineHeight = lineMaxFontSize * 1.2f;
                line.Baseline = y + lineMaxFontSize;
                layoutLines.Add(line);
                y += line.LineHeight;
            }
        }

        int requiredHeight = (int)Math.Max(minHeight, y + margin);

        using var image = new Image<Rgba32>(width, requiredHeight);
        image.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.FromRgb(255, 255, 255)));

        foreach (var line in layoutLines)
        {
            foreach (var (text, fontSize, bold, italic, color, wordX) in line.Words)
            {
                var font = GetFont(fontSize, bold, italic);
                float runBaseline = line.Baseline;

                image.Mutate(ctx =>
                {
                    ctx.DrawText(text, font, color,
                                 new SixLabors.ImageSharp.PointF(wordX, runBaseline));
                });
            }
        }

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    private Font GetFont(float size, bool bold, bool italic)
    {
        var key = (size, bold, italic);
        if (!_fontCache.ContainsKey(key))
        {
            if (_fontFamily == null)
            {
                if (bold && italic && _titleFontItalic != null)
                {
                    return _titleFontItalic;
                }
                if (bold && !italic && _titleFont != null)
                {
                    return _titleFont;
                }
                if (italic && !bold && _italicFont != null)
                {
                    return _italicFont;
                }
                if (_bodyFont != null)
                {
                    return _bodyFont;
                }
                key = (SS14_DEFAULT_SIZE, false, false);
                if (_fontCache.ContainsKey(key))
                {
                    return _fontCache[key];
                }
                throw new Exception("No fonts loaded");
            }

            FontStyle style = FontStyle.Regular;
            if (bold && italic)
            {
                style = FontStyle.Bold | FontStyle.Italic;
            }
            else if (bold)
            {
                style = FontStyle.Bold;
            }
            else if (italic)
            {
                style = FontStyle.Italic;
            }

            _fontCache[key] = new Font(_fontFamily.Value, size, style);
        }
        return _fontCache[key];
    }

    private void OnEject(Entity<BookPublisherComponent> ent, ref BookPublisherEjectMessage msg)
    {
        _itemSlots.TryEject(ent.Owner, "Book", msg.Actor, out var _);
        UpdateUi(ent);
    }

    private void OnRefresh(Entity<BookPublisherComponent> ent, ref BookPublisherRefreshMessage msg)
    {
        UpdateUi(ent);
    }

    private bool CanUse(EntityUid user, EntityUid machine)
    {
        if (TryComp<AccessReaderComponent>(machine, out var access))
            return _accessReader.IsAllowed(user, machine, access);
        return true;
    }

    private void UpdateUi(Entity<BookPublisherComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, BookPublisherUiKey.Key))
            return;

        var bookSlot = ent.Comp.BookSlot;
        var bookName = bookSlot.Item != null ? MetaData(bookSlot.Item.Value).EntityName : "";

        var state = new BookPublisherBoundUserInterfaceState(
            bookSlot.Item != null,
            bookName,
            ent.Comp.PublishEnabled,
            "");

        _ui.SetUiState(ent.Owner, BookPublisherUiKey.Key, state);
    }
}
