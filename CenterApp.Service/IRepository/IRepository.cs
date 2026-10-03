// Service/IRepository/IRepository.cs
using System.Linq.Expressions;
namespace CenterApp.Service.IRepository;

public interface IRepository<T> where T : class
{
    IQueryable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool tracking = false);
    T? GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracking = true);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
}