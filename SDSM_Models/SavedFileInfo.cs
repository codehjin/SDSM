using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 일괄저장된 파일이름 정보
/// </summary>
[Table("SavedFileInfo")]
public class SavedFileInfo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int SavedFileInfoId { get; set; }

    [DisplayName("구분")]
    public string Division { get; set; } = string.Empty;

    [DisplayName("파일명")]
    public string FileName { get; set; } = string.Empty;

    [DisplayName("저장일")]
    public DateTime SavedTime { get; set; }
}
