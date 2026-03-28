using Microsoft.EntityFrameworkCore;
using SixteenSounds.Models;

namespace SixteenSounds.Data
{
    public class SixteenSoundsDbContext : DbContext
    {
        public SixteenSoundsDbContext(DbContextOptions<SixteenSoundsDbContext> options)
            : base(options)
        {
        }

        // Tutaj definiujesz tabele, które pojawią się w bazie
        public DbSet<User> Users { get; set; }

        public DbSet<Sample> Samples { get; set; }

        public DbSet<Tag> Tags { get; set; }
    }
}