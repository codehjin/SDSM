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
        modelBuilder.Entity<BenthosView>(m => { m.HasNoKey(); });
        modelBuilder.Entity<BenthosSpeciesView>(m => { m.HasNoKey(); });
        modelBuilder.Entity<FishView>(m => { m.HasNoKey(); });
        modelBuilder.Entity<FishSpeciesView>(m => { m.HasNoKey(); });

        //Foregin Include
        //modelBuilder.Entity<FishSurveySpecies>().Navigation(e => e.FishSurvey).AutoInclude();
        //modelBuilder.Entity<BenthosSpecies>().Navigation(e => e.Benthos).AutoInclude();
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
    public DbSet<BenthosSpeciesList> BenthosSpeciesLists { get; set; }
    public DbSet<BenthosView> BenthosViews { get; set; }
    public DbSet<BenthosSpeciesView> BenthosSpeciesViews { get; set; }

    //어류
    public DbSet<Fish> Fishs { get; set; }
    public DbSet<FishSpecies> FishSpeciess { get; set; }
    public DbSet<FishSpeciesList> FishSpeciesLists { get; set; }
    public DbSet<FishView> FishViews { get; set; }
    public DbSet<FishSpeciesView> FishSpeciesViews { get; set; }

    //양서파충류
    public DbSet<AmphibianReptile> AmphibianReptiles { get; set; }

    public  DbSet<SelfUser> SelfUsers { get; set; }
}
