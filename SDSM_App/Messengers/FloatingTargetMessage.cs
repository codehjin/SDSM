namespace SDSM_App.Messengers;

public class FloatingTargetMessage(string paneName)
{
    public string PaneName { get; set; } = paneName;
}
