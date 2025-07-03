using SDSM_App.Enums;

namespace SDSM_App.Messengers;

/// <summary>
/// T : 작성모델
/// </summary>
/// <typeparam name="T"></typeparam>
public class EditorMessage<T>(T model, EditStatusType editStatus)
{
    public T Model { get; set; } = model;
    public EditStatusType EditStatus { get; set; } = editStatus;
}
