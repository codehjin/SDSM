namespace SDSM_App.Models.Repositories.Bases;

public interface IRepositoryBase<T> where T : class
{
    Task<T> InsertAsync(T model);
    Task<bool> UpdateAsync(T model);
    Task<bool> DeleteByIdAsync(int id);
    Task<bool> DeleteAsync(T model);
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();

    T Insert(T model);
    bool Update(T model);
    bool DeleteById(int id);
    bool Delete(T model);
    T GetById(int id);
    IEnumerable<T> GetAll();

    IQueryable<T> GetQueryable();
}
