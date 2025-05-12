using Azure;

namespace AzureDDNS;

public static class CollectionExtensions
{
    public static async Task<TSource?> FirstOrDefaultAsync<TSource>(this AsyncPageable<TSource> source,
                                                                    Func<TSource, bool> predicate,
                                                                    CancellationToken token = default)
            where TSource : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        token.ThrowIfCancellationRequested();

        await foreach (var item in source.ConfigureAwait(false))
        {
            token.ThrowIfCancellationRequested();
            if (predicate(item)) return item;
        }

        return default;
    }
}
