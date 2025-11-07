using Microsoft.EntityFrameworkCore;
using PruebaDV.Models;

namespace PruebaDV.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Ticket> Tickets { get; set; }
    }
}
