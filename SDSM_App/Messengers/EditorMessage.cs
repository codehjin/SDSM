namespace SDSM_App.Messengers;

/// <summary>
/// T : 작성모델
/// </summary>
/// <typeparam name="T"></typeparam>
public class EditorMessage<T>(T model)
{
    public T Model { get; set; } = model;
}
