using SDSM_App.InjectableServices;
using SDSM_App.Models.Repositories.Bases;
using SDSM_Models;

namespace SDSM_App.Models.Repositories.Interfases;

public interface ISiteDivisionRepository : IRepositoryBase<SiteDivision>, ITransientService { }
