namespace Content.Server.Discord._Persistence14;

public sealed class WebhookFile
{
    public string Filename { get; }
    public byte[] Data { get; }

    public WebhookFile(string filename, byte[] data)
    {
        Filename = filename;
        Data = data;
    }
}
