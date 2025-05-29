using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FRELODYAPP.Interfaces;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity;

namespace FRELODYAPP.Data.Infrastructure
{
    public partial class SongDbContext : IdentityDbContext<User>
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly string _tenantId;
        private readonly string _userId;

        public SongDbContext(DbContextOptions<SongDbContext> options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
            _tenantId = _tenantProvider.GetTenantId();
        }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<SongBook> SongBooks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Verse> Verses { get; set; }
        public DbSet<Bridge> Bridges { get; set; }
        public DbSet<Chorus> Choruses { get; set; }
        public DbSet<LyricLine> LyricLines { get; set; }
        public DbSet<LyricSegment> LyricSegments { get; set; }
        public DbSet<Chord> Chords { get; set; }
        public DbSet<ChordChart> ChordCharts { get; set; }
        public DbSet<UserFeedback> UserFeedback { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure global query filters for entities implementing IBaseEntity
            builder.Entity<Tenant>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<SongBook>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Category>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Song>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Verse>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Bridge>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Chorus>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<LyricLine>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<LyricSegment>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Chord>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<ChordChart>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<UserFeedback>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Page>().HasQueryFilter(x => (x.TenantId == _tenantId || x.TenantId == null) && (x.IsDeleted == false || x.IsDeleted == null));

            // Configure Song and its children
            builder.Entity<Song>()
                .HasMany(song => song.Verses)
                .WithOne()
                .HasForeignKey(verse => verse.SongId);

            builder.Entity<Song>()
                .HasMany(song => song.Choruses)
                .WithOne()
                .HasForeignKey(chorus => chorus.SongId);

            builder.Entity<Song>()
                .HasMany(song => song.Bridges)
                .WithOne()
                .HasForeignKey(bridge => bridge.SongId);

            builder.Entity<Verse>()
                .HasMany(verse => verse.LyricLines)
                .WithOne()
                .HasForeignKey(line => line.VerseId);

            builder.Entity<Chorus>()
                .HasMany(chorus => chorus.LyricLines)
                .WithOne()
                .HasForeignKey(line => line.ChorusId);

            builder.Entity<Bridge>()
                .HasMany(bridge => bridge.LyricLines)
                .WithOne()
                .HasForeignKey(line => line.BridgeId);

            builder.Entity<LyricLine>()
                .HasMany(line => line.LyricSegments)
                .WithOne()
                .HasForeignKey(segment => segment.LyricLineId);

            builder.Entity<UserFeedback>()
                .HasOne(fb => fb.Song)
                .WithMany(song => song.Feedback)
                .HasForeignKey(fb => fb.SongId);

            builder.Entity<ChordChart>()
                .HasOne<Chord>()
                .WithMany(chord => chord.ChordCharts)
                .HasForeignKey(chart => chart.ChordId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LyricSegment>()
                .HasOne(segment => segment.Chord)
                .WithMany(chord => chord.LyricSegments)
                .HasForeignKey(segment => segment.ChordId);

            // Configure SongBook and Category
            builder.Entity<Category>()
                .HasOne<SongBook>()
                .WithMany(songBook => songBook.Categories)
                .HasForeignKey(category => category.SongBookId);

            builder.Entity<Song>()
                .HasOne<Category>()
                .WithMany(category => category.Songs)
                .HasForeignKey(song => song.CategoryId);

            builder.Entity<Category>()
                .HasOne<Category>()
                .WithMany()
                .HasForeignKey(c => c.ParentCategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Chord properties to be stored as strings
            builder.Entity<Chord>()
                .Property(c => c.Difficulty)
                .HasConversion(new EnumToStringConverter<ChordDifficulty>());

            builder.Entity<Chord>()
                .Property(c => c.ChordType)
                .HasConversion(new EnumToStringConverter<ChordType>());

            builder.Entity<Song>()
                .Property(c => c.SongPlayLevel)
                .HasConversion(new EnumToStringConverter<PlayLevel>());

            builder.Entity<LyricLine>()
                .Property(e => e.PartName)
                .HasConversion<string>();

            builder.Entity<User>(b =>
            {
                b.HasIndex(u => u.TenantId);
                b.HasQueryFilter(u => u.TenantId == _tenantId);
            });

            foreach (var entityType in builder.Model.GetEntityTypes())
            {

                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // Apply index on IsDeleted
                    builder.Entity(entityType.ClrType)
                        .HasIndex(nameof(BaseEntity.IsDeleted));

                    //Apply index on TenantId
                    builder.Entity(entityType.ClrType)
                        .HasIndex(nameof(BaseEntity.TenantId));

                    if (entityType.ClrType != typeof(Tenant) && typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                    {
                        // Apply foreign key for TenantId in all entities that inherit from BaseEntity
                        builder.Entity(entityType.ClrType)
                            .HasOne(typeof(Tenant)) // Principal entity
                            .WithMany() // No navigation property in Tenant
                            .HasForeignKey(nameof(BaseEntity.TenantId)) // ForeignKey is TenantId
                            .IsRequired(false) // Set as not required (nullable)
                            .OnDelete(DeleteBehavior.Restrict); // Restrict delete behavior
                    }

                    // Apply index on DateTimeCreated
                    builder.Entity(entityType.ClrType)
                         .HasIndex(nameof(BaseEntity.DateCreated));

                    // Apply index on DateTimeModified
                    builder.Entity(entityType.ClrType)
                        .HasIndex(nameof(BaseEntity.DateModified));

                    // Apply index on LastModifiedBy
                    builder.Entity(entityType.ClrType)
                        .HasIndex(nameof(BaseEntity.ModifiedBy));

                }
            }

            builder.Entity<IdentityUserLogin<string>>().HasKey(x => new { x.LoginProvider, x.ProviderKey });
            builder.Entity<IdentityUserRole<string>>().HasKey(x => new { x.UserId, x.RoleId });
            builder.Entity<IdentityUserToken<string>>().HasKey(x => new { x.UserId, x.LoginProvider, x.Name });

            OnModelCreatingPartial(builder);
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }
        private void UpdateTimestamps()
        {
            var entities = ChangeTracker.Entries<IBaseEntity>();


            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    entity.Entity.DateCreated = DateTime.UtcNow;
                    entity.Entity.DateModified = DateTime.UtcNow;
                    entity.Entity.ModifiedBy = _userId;
                    entity.Entity.TenantId = entity.Entity.TenantId;
                }
                else if (entity.State == EntityState.Modified)
                {
                    entity.Entity.DateModified = DateTime.UtcNow;
                    entity.Property(p => p.DateCreated).IsModified = false;
                    entity.Entity.ModifiedBy = _userId;
                }
            }
        }
    }
}
