using SDSM_App.Models.Repositories.Bases;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;

namespace SDSM_App.Models.Repositories;

public class HabitatWaterEdgeRepository(SDSMDbContext context) : RepositoryBase<HabitatWaterEdge>(context), IHabitatWaterEdgeRepository { }
