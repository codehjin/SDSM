using SDSM_App.Models.Repositories.Bases;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;

namespace SDSM_App.Models.Repositories;

public class MammalRepository(SDSMDbContext context) : RepositoryBase<Mammal>(context), IMammalRepository { }
