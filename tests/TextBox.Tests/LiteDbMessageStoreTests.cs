using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using TextBox.Models;
using TextBox.Services;

namespace TextBox.Tests;

/// <summary>
/// Exercises the real LiteDB-backed store (file in a temp dir) including the
/// <see cref="IMemoryCache"/> read-through layer.
/// </summary>
public sealed class LiteDbMessageStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"textbox-tests-{Guid.NewGuid():N}");
    private readonly List<LiteDbMessageStore> _stores = [];
    private readonly List<MemoryCache> _caches = [];

    private LiteDbMessageStore CreateStore()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(_dir);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MessageStore:Path"] = "test.db" })
            .Build();
        var cache = new MemoryCache(new MemoryCacheOptions());
        _caches.Add(cache);
        var store = new LiteDbMessageStore(cache, env, config);
        _stores.Add(store);
        return store;
    }

    [Fact]
    public async Task Add_Get_RoundtripsMessage()
    {
        var store = CreateStore();

        var added = await store.AddAsync("+123", "TextBox", "hello");
        var fetched = await store.GetAsync(added.Id);

        Assert.NotNull(fetched);
        Assert.Equal(added.Id, fetched.Id);
        Assert.Equal("+123", fetched.To);
        Assert.Equal("hello", fetched.Body);
    }

    [Fact]
    public async Task Get_MissingId_ReturnsNull()
    {
        var store = CreateStore();

        Assert.Null(await store.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task List_ReturnsNewestFirst_AndFiltersCaseInsensitively()
    {
        var store = CreateStore();
        await store.AddAsync("+123", null, "first");
        await Task.Delay(20);
        await store.AddAsync("+123", null, "second");
        await Task.Delay(20);
        await store.AddAsync("+456", null, "other");

        var all = await store.ListAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("other", all[0].Body);

        var filtered = await store.ListAsync("+12");
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, m => Assert.Equal("+123", m.To));
    }

    [Fact]
    public async Task RapidAdds_ListNewestFirst_WithStrictlyDecreasingTimestamps()
    {
        // Back-to-back sends can share a clock tick; the store must still
        // order them by arrival so "last message" picks are deterministic.
        var store = CreateStore();
        const int count = 20;
        var ids = new List<Guid>(count);
        for (var i = 0; i < count; i++)
            ids.Add((await store.AddAsync("+t", null, $"m{i}")).Id);

        var list = (await store.ListAsync("+t")).ToList();

        Assert.Equal(count, list.Count);
        Assert.Equal(ids[^1], list[0].Id);
        Assert.Equal($"m{count - 1}", list[0].Body);
        AssertStrictlyDecreasing(list);
    }

    [Fact]
    public async Task ConcurrentAdds_HaveStrictlyDecreasingTimestamps()
    {
        // Concurrent sends land in the same tick; arrival order must still win.
        var store = CreateStore();
        const int count = 50;
        await Task.WhenAll(Enumerable.Range(0, count)
            .Select(i => store.AddAsync("+c", null, $"m{i}")));

        var list = (await store.ListAsync("+c")).ToList();

        Assert.Equal(count, list.Count);
        AssertStrictlyDecreasing(list);
    }

    private static void AssertStrictlyDecreasing(IReadOnlyList<SmsMessage> list)
    {
        for (var i = 1; i < list.Count; i++)
            Assert.True(list[i - 1].CreatedAt > list[i].CreatedAt,
                "timestamps must be strictly decreasing in listed order");
    }

    [Fact]
    public async Task List_SeesWrites_AfterCachedRead()
    {
        var store = CreateStore();
        await store.AddAsync("+1", null, "one");
        Assert.Single(await store.ListAsync());

        await store.AddAsync("+1", null, "two");

        Assert.Equal(2, (await store.ListAsync()).Count);
    }

    [Fact]
    public async Task Clear_RemovesAll_AndReturnsCount()
    {
        var store = CreateStore();
        await store.AddAsync("+1", null, "one");
        await store.AddAsync("+1", null, "two");
        Assert.Equal(2, (await store.ListAsync()).Count);

        var cleared = await store.ClearAsync();

        Assert.Equal(2, cleared);
        Assert.Empty(await store.ListAsync());
    }

    public void Dispose()
    {
        foreach (var store in _stores)
            store.Dispose();
        _stores.Clear();
        foreach (var cache in _caches)
            cache.Dispose();
        _caches.Clear();
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}
