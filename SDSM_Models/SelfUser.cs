using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 사용자
/// </summary>
[Table("SelfUser")]
public class SelfUser
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SelfUserId { get; set; }

    [DisplayName("사용자")]
    [StringLength(20)]
    [Required]
    public string UserName { get; set; } = string.Empty;

    [DisplayName("비밀번호")]
    [StringLength(20)]
    [Required]
    public string UserPassword { get; set; } = string.Empty;
}
