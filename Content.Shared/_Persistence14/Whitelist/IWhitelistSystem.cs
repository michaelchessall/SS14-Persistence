namespace Content.Shared._Persistence14.Whitelist;

public abstract class BaseWhitelistSystem<TWhitelist, TData> : EntitySystem
{
    public abstract bool WhitelistPass(TWhitelist? whitelist, TData data);
    public abstract bool WhitelistFail(TWhitelist? whitelist, TData data);

    public bool WhitelistPassOrNull(TWhitelist? whitelist, TData data)
        => whitelist is null || WhitelistPass(whitelist, data);
    public bool WhitelistFailOrNull(TWhitelist? whitelist, TData data)
        => whitelist is null || WhitelistPass(whitelist, data);

    public bool BlacklistPass(TWhitelist? blacklist, TData data) => WhitelistFail(blacklist, data);
    public bool BlacklistFail(TWhitelist? blacklist, TData data) => WhitelistPass(blacklist, data);
    public bool BlacklistPassorNull(TWhitelist? blacklist, TData data) => WhitelistFailOrNull(blacklist, data);
    public bool BlacklistFailOrNull(TWhitelist? blacklist, TData data) => WhitelistPassOrNull(blacklist, data);
}