// Service/Repository/Repository.cs
using CenterApp.Infrastructure.Data;
using CenterApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CenterApp.Infrastructure.Services;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext db) { _db = db; _set = db.Set<T>(); }

    public IQueryable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool tracking = false)
    {
        IQueryable<T> q = _set;
        if (!tracking) q = q.AsNoTracking();
        if (filter != null) q = q.Where(filter);
        if (!string.IsNullOrWhiteSpace(includeProperties))
            foreach (var inc in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                q = q.Include(inc);
        return q;
    }

    public T? GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracking = true)
        => GetAll(filter, includeProperties, tracking).FirstOrDefault();

    public void Add(T entity) => _set.Add(entity);
    public void AddRange(IEnumerable<T> entities) => _set.AddRange(entities);
    public void Update(T entity) => _set.Update(entity);
    public void Remove(T entity) => _set.Remove(entity);
}