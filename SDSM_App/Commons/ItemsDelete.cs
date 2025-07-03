using System.Data;
using System.Data.SQLite;
using System.Reflection;

namespace SDSM_App.Commons;

public class ItemsDelete<T> where T : class
{
    public bool Delete(List<T> items)
    {
        string strConn = GB.SqliteDataSource;
        using var conn = new SQLiteConnection(strConn);

        try
        {
            string id = $"{typeof(T).Name}Id";
            PropertyInfo[] props = typeof(T).GetProperties();
            var prop = props.Where(m => m.Name.Equals(id, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (prop != null)
            {
                conn.Open();

                using var transaction = conn.BeginTransaction();

                using var cmd = new SQLiteCommand(conn);
                cmd.Transaction = transaction;

                foreach (var item in items)
                {
                    var value = prop?.GetValue(item);
                    string strSql = $"DELETE FROM {typeof(T).Name} WHERE {id}=@{id};";

                    cmd.CommandText = strSql;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue($"@{id}", value);

                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                conn.Close();

                return true;
            }
            else
            {
                return false;
            }

        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, $"{nameof(Delete)} items", ex.Message);
            return false;
        }
        finally
        {
            if (conn.State == ConnectionState.Open)
                conn.Close();
        }
    }

    public bool Delete(T item)
    {
        string strConn = GB.SqliteDataSource;        
        using var conn = new SQLiteConnection(strConn);
        
        try
        {
            string id = $"{typeof(T).Name}Id";
            PropertyInfo[] props = typeof(T).GetProperties();
            var prop = props.Where(m => m.Name.Equals(id, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (prop != null)
            {
                conn.Open();

                using var cmd = new SQLiteCommand(conn);

                var value = prop?.GetValue(item);
                string strSql = $"DELETE FROM {typeof(T).Name} WHERE {id}=@{id};";

                cmd.CommandText = strSql;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue($"@{id}", value);

                cmd.ExecuteNonQuery();
                
                conn.Close();

                return true;
            }
            else
            {
                return false;
            }

        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, $"{nameof(Delete)} item", ex.Message);
            return false;
        }
        finally
        {
            if(conn.State == ConnectionState.Open)
                conn.Close();
        }
    }
}
