using Microsoft.EntityFrameworkCore;
using Swallows.Core.Models;

namespace Swallows.Core.Data;

public class AppDbContext : DbContext
{
    public DbSet<ScanSession> ScanSessions { get; set; } = null!;
    public DbSet<Page> Pages { get; set; } = null!;
    public DbSet<Link> Links { get; set; } = null!;
    public DbSet<ImageAsset> ImageAssets { get; set; } = null!;
    public DbSet<AppSettings> AppSettings { get; set; } = null!;
    
    public string DbPath { get; private set; }

    public AppDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "swallows.db");
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        // For testing/memory DB
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }
    
    public void UpgradeSchemaIfNeeded()
    {
        Database.EnsureCreated();
        
        // Manual patch for ImageAssets table if it was missing in existing DB
        try
        {
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""ImageAssets"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ImageAssets"" PRIMARY KEY AUTOINCREMENT,
                    ""Url"" TEXT NOT NULL,
                    ""AltText"" TEXT NULL,
                    ""PageId"" INTEGER NOT NULL,
                    CONSTRAINT ""FK_ImageAssets_Pages_PageId"" FOREIGN KEY (""PageId"") REFERENCES ""Pages"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_ImageAssets_PageId"" ON ""ImageAssets"" (""PageId"");
            ");
        }
        catch 
        {
            // Ignore if already exists or other error, trust EF mostly
        }
        
        // Add new columns to ImageAssets if they don't exist
        try { Database.ExecuteSqlRaw(@"ALTER TABLE ImageAssets ADD COLUMN Title TEXT NULL;"); } catch { }
        try { Database.ExecuteSqlRaw(@"ALTER TABLE ImageAssets ADD COLUMN SizeBytes INTEGER NULL;"); } catch { }
        try { Database.ExecuteSqlRaw(@"ALTER TABLE ImageAssets ADD COLUMN ContentHash TEXT NULL;"); } catch { }
        
        // Add SaveImages column to AppSettings
        try
        {
            Database.ExecuteSqlRaw(@"ALTER TABLE AppSettings ADD COLUMN SaveImages INTEGER NOT NULL DEFAULT 0;");
        }
        catch { /* Ignore if column already exists */ }
    }
}
