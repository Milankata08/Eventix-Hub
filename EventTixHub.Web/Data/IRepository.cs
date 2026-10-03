using EventTixHub.Web.Models;

namespace EventTixHub.Web.Data;

public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(); // commits the request's shared DbContext = unit of work
}
