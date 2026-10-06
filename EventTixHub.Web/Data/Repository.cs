using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Data;

public class Repository<T>(ApplicationDbContext db) : IRepository<T> where T : BaseEntity
{
    public IQueryable<T> Query() => db.Set<T>();

    public async Task<T?> GetByIdAsync(int id) => await db.Set<T>().FindAsync(id);

    public async Task AddAsync(T entity) => await db.Set<T>().AddAsync(entity);

    public void Update(T entity) => db.Set<T>().Update(entity);

    public void Remove(T entity) => db.Set<T>().Remove(entity);

    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
}
