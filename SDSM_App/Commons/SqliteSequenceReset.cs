using System.Data;
using System.Data.SQLite;

namespace SDSM_App.Commons;

public class SqliteSequenceReset
{
    /// <summary>
    /// Sqlite 시퀀스 리셋
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    public static async Task SqliteSequenceResetAsync(string tableName)
    {
        string strConn = GB.SqliteDataSource;
        using var conn = new SQLiteConnection(strConn);
        try
        {
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            using var cmd = new SQLiteCommand();
            cmd.Transaction = transaction;
       
            cmd.CommandText = "DELETE FROM \"sqlite_sequence\" WHERE name=@name";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@name", tableName);

            await cmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            await conn.CloseAsync();
        }
        catch (Exception ex)
        {
            GB.LogWrite(nameof(SqliteSequenceReset), nameof(SqliteSequenceResetAsync), ex.Message);
        }
        finally
        {
            if (conn.State == ConnectionState.Open)
                await conn.CloseAsync();
        }
    }
}
