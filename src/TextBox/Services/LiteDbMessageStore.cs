using System.Collections.Concurrent;
using LiteDB;
using LiteDB.Async;
using Microsoft.Extensions.Caching.Memory;
using TextBox.Models;

namespace TextBox.Services;

/// <summary>
/// LiteDB-backed store (LiteDB.Async extensions) with an <see cref="IMemoryCache"/>
/// read-through layer. Writes invalidate the cached list/count entries.
/// </summary>
public sealed class LiteDbMessageStore : IMessageStore, IDisposable
{
    private const string CollectionName = "messages";

    private static readonly TimeSpan ListTtl = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ItemTtl = TimeSpan.FromMinutes(1);

    private readonly LiteDatabaseAsync _db;
    private readonly ILiteCollectionAsync<SmsMessage> _collection;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _listKeys = new();
    private readonly ConcurrentDictionary<string, byte> _itemKeys = new();
    private readonly Lazy<Task> _init;
    private readonly Lock _timestampLock = new();
    private DateTimeOffset _lastIssued;
    private bool _disposed;

    public LiteDbMessageStore(IMemoryCache cache, IWebHostEnvironment env, IConfiguration config)
    {
        _cache = cache;
        var path = config.GetSection(MessageStoreOptions.SectionName).Get<MessageStoreOptions>()
            ?.Path ?? new MessageStoreOptions().Path;
        var full = Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        _db = new LiteDatabaseAsync($"Filename={full};Connection=shared");
        _collection = _db.GetCollection<SmsMessage>(CollectionName);
        _init = new Lazy<Task>(InitAsync);
    }

    private async Task InitAsync()
    {
        await _collection.EnsureIndexAsync(x => x.To);
        await _collection.EnsureIndexAsync(x => x.CreatedAt);
    }

    private Task ReadyAsync() => _init.Value;

    private static string ItemKey(Guid id) => $"messages:item:{id:N}";

    private static string ListKey(string? toFilter) =>
        $"messages:list:{(string.IsNullOrWhiteSpace(toFilter) ? "<all>" : toFilter.Trim().ToLowerInvariant())}";

    private void EvictListCaches()
    {
        foreach (var key in _listKeys.Keys)
            _cache.Remove(key);
        _listKeys.Clear();
    }

    private void EvictAllCaches()
    {
        EvictListCaches();
        foreach (var key in _itemKeys.Keys)
            _cache.Remove(key);
        _itemKeys.Clear();
    }

    /// <summary>
    /// Issues strictly increasing timestamps at the millisecond granularity
    /// LiteDB persists. Back-to-back sends can share a clock tick, and ties
    /// make "last message" picks depend on storage order — so each timestamp
    /// is clamped past the previous one. Under burst load this can run ahead
    /// of wall-clock time by a few milliseconds; acceptable for a mock inbox.
    /// </summary>
    private DateTimeOffset NextTimestamp()
    {
        lock (_timestampLock)
        {
            var now = TruncateToMilliseconds(DateTimeOffset.UtcNow);
            if (now <= _lastIssued)
                now = _lastIssued.AddMilliseconds(1);
            _lastIssued = now;
            return now;
        }
    }

    private static DateTimeOffset TruncateToMilliseconds(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMillisecond));

    public async Task<SmsMessage> AddAsync(string to, string? from, string body, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await ReadyAsync();
        var message = new SmsMessage
        {
            Id = Guid.NewGuid(),
            To = to,
            From = from,
            Body = body,
            CreatedAt = NextTimestamp()
        };
        await _collection.InsertAsync(message);
        // New rows invalidate cached lists, but the item itself stays cached.
        EvictListCaches();
        _cache.Set(ItemKey(message.Id), message, ItemTtl);
        _itemKeys.TryAdd(ItemKey(message.Id), 0);
        return message;
    }

    public async Task<IReadOnlyList<SmsMessage>> ListAsync(string? toFilter = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = ListKey(toFilter);
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            _listKeys.TryAdd(key, 0);
            entry.SlidingExpiration = ListTtl;
            await ReadyAsync();
            var all = await _collection.FindAllAsync();
            return all.SortedFiltered(toFilter);
        }) ?? [];
    }

    public async Task<SmsMessage?> GetAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = ItemKey(id);
        var message = await _cache.GetOrCreateAsync(key, async entry =>
        {
            _itemKeys.TryAdd(key, 0);
            entry.SlidingExpiration = ItemTtl;
            await ReadyAsync();
            return await _collection.FindByIdAsync(new BsonValue(id));
        });
        if (message is null)
            _cache.Remove(key);
        return message;
    }

    public async Task<int> ClearAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await ReadyAsync();
        var cleared = await _collection.DeleteAllAsync();
        EvictAllCaches();
        return cleared;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _db.Dispose();
    }
}