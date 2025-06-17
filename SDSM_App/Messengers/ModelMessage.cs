using SDSM_App.Enums;

namespace SDSM_App.Messengers;

public class ModelMessage<T>
{
    /// <summary>
    /// 작성폼에 전달정보
    /// </summary>
    /// <param name="model">대상모델</param>
    public ModelMessage(T model) => Model = model;

    public T Model { get; set; }
}
