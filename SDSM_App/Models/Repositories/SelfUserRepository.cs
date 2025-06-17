using SDSM_App.Models.Repositories.Bases;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;

namespace SDSM_App.Models.Repositories;

public class SelfUserRepository(SDSMDbContext context) : RepositoryBase<SelfUser>(context), ISelfUserRepository { }
