using Microsoft.EntityFrameworkCore;
using Api;

namespace Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Zone> Zones { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=myweatherhub;Username=myuser;Password=mypassword");
        }
    }
}
