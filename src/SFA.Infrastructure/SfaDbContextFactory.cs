using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SFA.Infrastructure;

public class SfaDbContextFactory : IDesignTimeDbContextFactory<SfaDbContext>
{
  public SfaDbContext CreateDbContext(string[] args)
  {
    var cs = "Host=localhost;Port=5432;Database=sfa_db;Username=sfa;Password=sfa";
    var opt = new DbContextOptionsBuilder<SfaDbContext>()
      .UseNpgsql(cs, b => b.MigrationsAssembly("SFA.Infrastructure"))
      .UseSnakeCaseNamingConvention()
      .Options;
    return new SfaDbContext(opt);
  }
}
