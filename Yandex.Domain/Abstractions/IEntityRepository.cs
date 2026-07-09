using Yandex.Domain.Entities;

namespace Yandex.Domain.Abstractions;

public interface IEntityRepository<T> where T : BaseEntity
{
    T? GetById(Guid id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Remove(Guid id);
}