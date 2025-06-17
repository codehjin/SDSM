using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace SDSM_App.Models.Repositories.Bases;

public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected readonly DbContext Context;
    protected readonly DbSet<T> DbSet;

    public RepositoryBase(DbContext context)
    {
        Context = context;
        DbSet = Context.Set<T>();
    }

    #region 추가
    public async Task<T> InsertAsync(T model)
    {
        try
        {
            DbSet.Add(model);
            int count = await Context.SaveChangesAsync();

            return count > 0 ? model : null!;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(InsertAsync), ex.Message);
            return null!;
        }
    }

    public T Insert(T model)
    {
        try
        {
            DbSet.Add(model);
            int count = Context.SaveChanges();

            return count > 0 ? model : null!;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(Insert), ex.Message);
            return null!;
        }
    }

    #endregion

    #region 수정

    public async Task<bool> UpdateAsync(T model)
    {
        try
        {
            Context.Entry(model).State = EntityState.Modified;
            return await Context.SaveChangesAsync() > 0;

        }
        catch (DbUpdateConcurrencyException)
        {
            var message = $"자료가 수정 또는 삭제되어 저장하실 수 없습니다{Environment.NewLine}목록을 다시 불러옵니다.";
            MessageBox.Show(message, "동시성 충돌 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(UpdateAsync), ex.Message);
            return false;
        }
    }

    public bool Update(T model)
    {
        try
        {
            Context.Entry(model).State = EntityState.Modified;
            return Context.SaveChanges() > 0;
        }
        catch (DbUpdateConcurrencyException)
        {
            var message = $"자료가 수정 또는 삭제되어 저장하실 수 없습니다{Environment.NewLine}목록을 다시 불러옵니다.";
            MessageBox.Show(message, "동시성 충돌 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(Update), ex.Message);
            return false;
        }
    }

    #endregion

    #region 삭제

    public async Task<bool> DeleteAsync(T model)
    {
        try
        {
            DbSet.Remove(model);
            int count = await Context.SaveChangesAsync();

            return count > 0;

        }
        catch (DbUpdateConcurrencyException)
        {
            var message = $"자료가 수정 또는 삭제되어 저장하실 수 없습니다{Environment.NewLine}목록을 다시 불러옵니다.";
            MessageBox.Show(message, "동시성 충돌 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(DeleteAsync), ex.Message);
            return false;
        }
    }

    public async Task<bool> DeleteByIdAsync(int id)
    {
        try
        {
            T? model = await DbSet.FindAsync(id);
            if (model != null)
            {
                return await DeleteAsync(model);
            }
            return false;

        }
        catch (DbUpdateConcurrencyException)
        {
            var message = $"자료가 수정 또는 삭제되어 저장하실 수 없습니다{Environment.NewLine}목록을 다시 불러옵니다.";
            MessageBox.Show(message, "동시성 충돌 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(DeleteByIdAsync), ex.Message);
            return false;
        }
    }

    public bool Delete(T model)
    {
        try
        {
            DbSet.Remove(model);
            int count = Context.SaveChanges();

            return count > 0;

        }
        catch (DbUpdateConcurrencyException)
        {
            var message = $"자료가 수정 또는 삭제되어 저장하실 수 없습니다{Environment.NewLine}목록을 다시 불러옵니다.";
            MessageBox.Show(message, "동시성 충돌 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(Delete), ex.Message);
            return false;
        }
    }

    public bool DeleteById(int id)
    {
        try
        {
            T? model = DbSet.Find(id);
            if (model != null)
            {
                return Delete(model);
            }
            return false;

        }
        catch (DbUpdateConcurrencyException)
        {
            var message = $"자료가 수정 또는 삭제되어 저장하실 수 없습니다{Environment.NewLine}목록을 다시 불러옵니다.";
            MessageBox.Show(message, "동시성 충돌 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(DeleteById), ex.Message);
            return false;
        }
    }

    #endregion

    #region 읽기

    public async Task<T> GetByIdAsync(int id)
    {
        try
        {
            T? model = await DbSet.FindAsync(id);
            if (model != null)
            {
                return model;
            }
            return null!;

        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(GetByIdAsync), ex.Message);
            return null!;
        }
    }

    public T GetById(int id)
    {
        try
        {
            T? model = DbSet.Find(id);
            if (model != null)
            {
                return model;
            }
            return null!;

        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(GetById), ex.Message);
            return null!;
        }
    }

    #endregion

    #region 목록 읽기

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await DbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(GetAllAsync), ex.Message);
            return Enumerable.Empty<T>();
        }

    }

    public IEnumerable<T> GetAll()
    {
        try
        {
            return DbSet.ToList();
        }
        catch (Exception ex)
        {
            GB.LogWrite(typeof(T).Name, nameof(GetAll), ex.Message);
            return Enumerable.Empty<T>();
        }

    }

    #endregion

    //AsQueryable 메서드는 메모리 내에서 데이터에 대한 궈리 가능한 개체를 만들기 때문에 비동기 작업이 필요하지 않음.
    public IQueryable<T> GetQueryable()
    {
        return DbSet.AsQueryable();
    }
}
