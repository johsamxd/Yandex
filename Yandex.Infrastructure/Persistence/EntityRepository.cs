using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.Infrastructure.Persistence;

public class EntityRepository<T> : IEntityRepository<T> where T : BaseEntity
{
    private readonly List<T> _items = [];

    public T? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);

    public IEnumerable<T> GetAll() => _items.AsEnumerable();

    public void Add(T entity) => _items.Add(entity);

    public void Update(T entity)
    {
        var existing = _items.FirstOrDefault(x => x.Id == entity.Id);
        if (existing == null)
            throw new KeyNotFoundException($"Entity with id {entity.Id} not found");

        var index = _items.IndexOf(existing);
        _items[index] = entity;
    }

    public void Remove(Guid id)
    {
        var existing = _items.FirstOrDefault(x => x.Id == id);
        if (existing == null)
            throw new KeyNotFoundException($"Entity with id {id} not found");

        _items.Remove(existing);
    }
}