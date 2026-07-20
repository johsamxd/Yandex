using System.Collections.Concurrent;
using Yandex.Domain.Entities;

namespace Yandex.Infrastructure.Persistence;

public class LocalDatabase
{
    private readonly ConcurrentDictionary<Type, object> _collections = new();
    
    public List<T> GetCollection<T>() where T : BaseEntity
    {
        var type = typeof(T);
        
        if (!_collections.TryGetValue(type, out var collection))
        {
            collection = new List<T>();
            _collections.TryAdd(type, collection);
        }
        
        return (List<T>)collection;
    }
}