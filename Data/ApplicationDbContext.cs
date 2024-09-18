using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tennis_Finder_App.Models;

namespace Tennis_Finder_App.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // DbSet for TennisCourt entity
        public DbSet<TennisCourt> TennisCourts { get; set; }
    }
}
