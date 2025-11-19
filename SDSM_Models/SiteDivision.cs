using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 지점명 구분
/// </summary>
[Table("SiteDivision")]
public class SiteDivision
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int SiteDivisionId { get; set; }

    [DisplayName("프로젝트명")] public string Project { get; set; } = string.Empty;
    [DisplayName("사업장")] public string? Workplace { get; set; }
    [DisplayName("과업 대표하천")] public string? WorkplaceRiver { get; set; }
    [DisplayName("지점명")] public string? WorkplaceSite { get; set; }
}
