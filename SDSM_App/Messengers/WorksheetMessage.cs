using Telerik.Windows.Documents.Spreadsheet.Model;

namespace SDSM_App.Messengers;

public class WorksheetMessage(Worksheet? worksheet = null)
{
    public Worksheet? Worksheet { get; set; } = worksheet;
}
