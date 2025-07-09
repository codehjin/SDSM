using SDSM_App.Models.Repositories.Bases;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;

namespace SDSM_App.Models.Repositories;

public class BenthosRepository(SDSMDbContext context) : RepositoryBase<Benthos>(context), IBenthosRepository { }
