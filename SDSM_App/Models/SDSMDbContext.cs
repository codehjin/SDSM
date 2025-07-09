using SDSM_App.InjectableServices;
using Microsoft.EntityFrameworkCore;
using SDSM_Models;

namespace SDSM_App.Models;

public class SDSMDbContext : DbContext, ITransientService
{
    public SDSMDbContext() : base()
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(GB.SqliteDataSource);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //No Key
        modelBuilder.Entity<FishSurveyView>(m => { m.HasNoKey(); });

        //Foregin Include
        modelBuilder.Entity<FishSurveySpecies>().Navigation(e => e.FishSurvey).AutoInclude();
        //modelBuilder.Entity<VeSurveyT2>().Navigation(e => e.VeSurvey).AutoInclude();
        //modelBuilder.Entity<VeSurveyS>().Navigation(e => e.VeSurvey).AutoInclude();
        //modelBuilder.Entity<VeSurveyH>().Navigation(e => e.VeSurvey).AutoInclude();
    }

    //포유류
    public DbSet<Mammal> Mammals { get; set; }
    //조류
    public DbSet<Bird> Birds { get; set; }
    //서식수변
    public DbSet<HabitatWaterEdge> HabitatWaterEdges { get; set; }
    //수질
    public DbSet<WaterQuality> WaterQualitys { get; set; }
    //저서동물
    public DbSet<Benthos> Benthoss { get; set; }
    public DbSet<BenthosSpecies> BenthosSpeciess { get; set; }

    //어류
    public  DbSet<FishSurvey> FishSurveys { get; set; }
    public  DbSet<FishSpecies> FishSpeciess { get; set; }
    public DbSet<FishSurveySpecies> FishSurveySpeciess { get; set; }
    public DbSet<FishSurveyView> FishSurveyViews { get; set; }
    //양서파충류
    public DbSet<AmphibianReptile> ARSurveys { get; set; }

    public  DbSet<SelfUser> SelfUsers { get; set; }
}
