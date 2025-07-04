using SDSM_App.Models.Repositories.Bases;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;

namespace SDSM_App.Models.Repositories;

public class BirdRepository(SDSMDbContext context) : RepositoryBase<Bird>(context), IBirdRepository { }
