using FRELODYAPP.Interfaces;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.Models;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Emit;

namespace FRELODYAPP.Data.Infrastructure
{
    public partial class SongDbContext : IdentityDbContext<User>
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly string _tenantId;
        private readonly string _userId;
        private readonly bool _isSuperAdmin;

        public SongDbContext(DbContextOptions<SongDbContext> options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
            _tenantId = _tenantProvider.GetTenantId();
            _isSuperAdmin = _tenantProvider.IsSuperAdmin();
        }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<SongCollection> SongCollections { get; set; }
        public DbSet<SongBook> SongBooks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<SongPart> SongParts { get; set; }
        public DbSet<SongUserRating> SongUserRatings { get; set; }
        public DbSet<LyricLine> LyricLines { get; set; }
        public DbSet<LyricSegment> LyricSegments { get; set; }
        public DbSet<Chord> Chords { get; set; }
        public DbSet<ChordChart> ChordCharts { get; set; }
        public DbSet<UserFeedback> UserFeedback { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Artist> Artists { get; set; } = default!;
        public DbSet<Album> Albums { get; set; } = default!;
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<ShareLink> ShareLinks { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SongPlayHistory> SongPlayHistories { get; set; }
        public DbSet<SongUserFavorite> SongUserFavorites { get; set; }
        public DbSet<SongUserCollection> SongUserCollections { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure global query filters for entities implementing IBaseEntity
            builder.Entity<SongCollection>().HasQueryFilter(x =>
                   (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                   && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<SongBook>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Category>().HasQueryFilter(x =>
                     (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Song>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<SongPart>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<LyricLine>().HasQueryFilter(x =>
                     (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<LyricSegment>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Artist>().HasQueryFilter(x =>
               (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
               && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Album>().HasQueryFilter(x =>
                (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null || x.Access == Access.Public)
                && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Setting>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Chord>().HasQueryFilter(x => 
                    x.IsDeleted == false || x.IsDeleted == null);
            builder.Entity<ChordChart>().HasQueryFilter(x =>
                   x.IsDeleted == false || x.IsDeleted == null);
            builder.Entity<UserFeedback>().HasQueryFilter(x => 
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null) 
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Page>().HasQueryFilter(x => 
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null) 
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<ShareLink>().HasQueryFilter(x => 
                    x.IsActive != false);
            builder.Entity<SongUserRating>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<SongPlayHistory>().HasQueryFilter(x =>
                   (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                   && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<SongUserCollection>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<SongUserFavorite>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<UserRefreshToken>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<User>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null)
                    && (x.IsActive == true || x.IsActive == null));
            builder.Entity<ChatMessage>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<ChatSession>().HasQueryFilter(x =>
                    (_isSuperAdmin || x.TenantId == _tenantId || x.TenantId == null)
                    && (x.IsDeleted == false || x.IsDeleted == null));
            builder.Entity<Tenant>().HasQueryFilter(x =>
                    (x.IsDeleted == false || x.IsDeleted == null));

            // Configure Song and its children
            builder.Entity<Song>()
                .HasMany(song => song.SongParts)
                .WithOne()
                .HasForeignKey(verse => verse.SongId);

            builder.Entity<SongPart>()
                .HasMany(verse => verse.LyricLines)
                .WithOne()
                .HasForeignKey(line => line.PartId);

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
            builder.Entity<SongBook>()
              .HasOne(sb => sb.Collection)
              .WithMany(c => c.SongBooks)
              .HasForeignKey(sb => sb.CollectionId);

            builder.Entity<Category>()
                .HasOne<SongBook>()
                .WithMany(songBook => songBook.Categories)
                .HasForeignKey(category => category.SongBookId);

            builder.Entity<Song>()
                .HasOne<Category>()
                .WithMany(category => category.Songs)
                .HasForeignKey(song => song.CategoryId);

            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Album>()
               .HasOne<Artist>()
               .WithMany(a => a.Albums)
               .HasForeignKey(al => al.ArtistId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Song>()
                .HasOne<Album>()
                .WithMany(al => al.Songs)
                .HasForeignKey(s => s.AlbumId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SongPlayHistory>(b =>
            {
                b.HasOne<Song>()
                 .WithMany()
                 .HasForeignKey(h => h.SongId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(h => h.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(h => new { h.SongId, h.UserId });
                b.HasIndex(h => h.PlayedAt);
                b.HasIndex(h => h.PlaySource);
            });

            builder.Entity<SongUserFavorite>(b =>
            {
                b.HasOne(f => f.Song)
                 .WithMany()
                 .HasForeignKey(f => f.SongId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(f => f.User)
                 .WithMany()
                 .HasForeignKey(f => f.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(f => new { f.SongId, f.UserId }).IsUnique();
                b.HasIndex(f => f.FavoritedAt);
            });

            // Configure Chord properties to be stored as strings
            builder.Entity<Chord>()
                .Property(c => c.Difficulty)
                .HasConversion<string>();

            builder.Entity<Chord>()
                .Property(c => c.ChordType)
                .HasConversion<string>();

            builder.Entity<Song>()
                .Property(c => c.SongPlayLevel)
                .HasConversion<string>();

            builder.Entity<SongPart>()
                .Property(e => e.PartName)
                .HasConversion<string>();
            
            builder.Entity<UserFeedback>()
                .Property(e => e.Status)
                .HasConversion<string>();
            
            builder.Entity<LyricSegment>()
                .Property(e => e.ChordAlignment)
                .HasConversion<string>();

            builder.Entity<Setting>(s =>
            {
                s.Property(e => e.PlayLevel)
                .HasConversion<string>();

                s.Property(e=>e.SongDisplay)
                .HasConversion<string>();

                s.Property(e=>e.ChordDisplay)
                .HasConversion<string>();

                s.Property(e => e.Theme)
                .HasConversion<string>();

                s.Property(e => e.ChordDifficulty)
                .HasConversion<string>();
            });

            // Configure ShareLink relationships
            builder.Entity<ShareLink>()
                .HasOne(sl => sl.Song)
                .WithMany()
                .HasForeignKey(sl => sl.SongId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ShareLink>()
                .HasIndex(sl => sl.ShareToken)
                .IsUnique();

            builder.Entity<ShareLink>()
                .HasIndex(sl => sl.CreatedAt);

            builder.Entity<ShareLink>()
                .HasIndex(sl => sl.ExpiresAt);
            
            builder.Entity<User>(b =>
            {
                b.HasIndex(u => u.TenantId);
                b.Property(e => e.UserType)
                .HasConversion<string>();
            });

            builder.Entity<SongUserRating>(b =>
            {
                b.HasOne<Song>()
                 .WithMany()
                 .HasForeignKey(r => r.SongId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(r => r.UserId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict); 
            });

            builder.Entity<SongUserCollection>(b =>
            {
                b.HasOne(suc => suc.Song)
                 .WithMany()
                 .HasForeignKey(suc => suc.SongId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(suc => suc.SongCollection)
                 .WithMany()
                 .HasForeignKey(suc => suc.SongCollectionId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(suc => new { suc.SongId, suc.SongCollectionId, suc.TenantId })
                 .IsUnique();

                b.HasIndex(suc => suc.AddedByUserId);
                b.HasIndex(suc => suc.DateScheduled);
                b.HasIndex(suc => suc.DateCreated);
            });

            builder.Entity<ChatMessage>(b =>
            {
                b.HasOne<ChatSession>()
                 .WithMany(cs => cs.Messages)
                 .HasForeignKey(cm => cm.ChatSessionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ChatSession>()
                .Property(e => e.Status)
                .HasConversion<string>();

            foreach (var entityType in builder.Model.GetEntityTypes())
            {

                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var entityBuilder = builder.Entity(entityType.ClrType);

                    entityBuilder.HasIndex(nameof(BaseEntity.IsDeleted));
                    entityBuilder.HasIndex(nameof(BaseEntity.DateCreated));
                    entityBuilder.HasIndex(nameof(BaseEntity.DateModified));
                    entityBuilder.HasIndex(nameof(BaseEntity.ModifiedBy));
                    entityBuilder.HasIndex(nameof(BaseEntity.TenantId));
                    
                    if (entityType.FindProperty(nameof(BaseEntity.Access)) != null)
                        entityBuilder.HasIndex(nameof(BaseEntity.Access));

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
                    if (string.IsNullOrEmpty(entity.Entity.TenantId) && !_isSuperAdmin)
                    {
                        entity.Entity.TenantId = _tenantId;
                    }
                    entity.Entity.CreatedBy = _userId;
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
