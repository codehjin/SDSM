namespace SDSM_App.Commons;

public static class PermanentlyDeleteFiles
{
    public static void DeleteFiles(List<string> files)
    {
		try
		{
            foreach (var file in files)
            {
                // Check if the file exists
                if (System.IO.File.Exists(file))
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file,
                     Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                     Microsoft.VisualBasic.FileIO.RecycleOption.DeletePermanently);
                }                
            }
		}
		catch { }
    }
}
