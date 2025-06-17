using SDSM_App.Models;

namespace SDSM_App.Commons;

public static class NetworkServerExtensions
{

    //DB 연결 상태
    public static bool IsDatabaseAvailable()
    {
        try
        {
            using var context = new SDSMDbContext();
            var result = context.Database.CanConnect();
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //DB 연결 상태
    public static async Task<bool> IsDatabaseAvailableAsync()
    {
        try
        {
            using var context = new SDSMDbContext();
            var result = await Task.Run(async () => await context.Database.CanConnectAsync());
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }
}