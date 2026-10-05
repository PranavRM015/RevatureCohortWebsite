using Microsoft.EntityFrameworkCore.Design;

// Used only by `dotnet ef` so it can create the context without running Program.cs (which starts the Discord bot).
public class CohortContextFactory : IDesignTimeDbContextFactory<CohortContext>
{
    public CohortContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.TraversePath().Load(); // CohortContext reads CONNECTION_STRING from the environment
        return new CohortContext();
    }
}
