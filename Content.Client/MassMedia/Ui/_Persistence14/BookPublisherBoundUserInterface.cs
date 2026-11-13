using Content.Shared.MassMedia.Components._Persistence14;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.MassMedia.Ui._Persistence14;

public sealed class BookPublisherBoundUserInterface : BoundUserInterface
{
    private BookPublisherWindow? _window;

    public BookPublisherBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BookPublisherWindow>();
        _window.PublishButtonPressed += OnPublish;
        _window.EjectButtonPressed += OnEject;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not BookPublisherBoundUserInterfaceState cast)
            return;

        _window?.UpdateUI(cast.BookInserted, cast.BookName, cast.CanPublish);
    }

    private void OnPublish(string title)
    {
        SendMessage(new BookPublisherPublishMessage(title));
    }

    private void OnEject()
    {
        SendMessage(new BookPublisherEjectMessage());
    }
}
