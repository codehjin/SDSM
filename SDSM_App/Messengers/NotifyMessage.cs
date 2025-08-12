namespace SDSM_App.Messengers;

public class NotifyMessage
{
    public NotifyMessage(object obj)
    {
        if(obj != null) 
        {
            var tuple = (ValueTuple<string, bool>)obj;
            Message = tuple.Item1;
            IsSucess = tuple.Item2;
        }            
    }

    public string Message { get; set; } = string.Empty;
    public bool IsSucess { get; set; }
}
