namespace SDSM_App.InjectableServices;

public interface IInjectablesService { }
public interface ITransientService : IInjectablesService { }
public interface IScopedService : IInjectablesService { }
public interface ISingletonService : IInjectablesService { }