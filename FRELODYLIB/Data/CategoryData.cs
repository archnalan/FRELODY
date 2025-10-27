using DocumentFormat.OpenXml.InkML;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.SubDtos;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FRELODYAPP.Models
{
    public static class CategoryData
    {
        public static async Task<bool> Initialize(IServiceProvider serviceProvider, string songBookId, string tenantId)
        {
           
            using (var context = new SongDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<SongDbContext>>(),
                serviceProvider.GetRequiredService<ITenantProvider>()))
            {
                if (context.Categories.Any()) return true;

                if (!context.SongBooks.Any(c => c.Id == songBookId))
                {
                    Debug.WriteLine("Categories not seeded, songbook of Id: {id} not found",songBookId);
                    return false;
                }
                Debug.WriteLine("Seeding categories...");

                var worshipId = Guid.NewGuid().ToString();
                var godTheFatherId = Guid.NewGuid().ToString();
                var jesusChristId = Guid.NewGuid().ToString();
                var gospelId = Guid.NewGuid().ToString();
                var christianChurchId = Guid.NewGuid().ToString();
                var doctrinesId = Guid.NewGuid().ToString();
                var christianLifeId = Guid.NewGuid().ToString();
                var christianHomeId = Guid.NewGuid().ToString();

                //1. Seeding parent categories first
                using var transaction = context.Database.BeginTransaction();

                context.Categories.AddRange(
                    new Category { Id = worshipId, Name = "WORSHIP", ParentCategoryId = null, SongBookId = songBookId,TenantId=tenantId, Access=Access.Public },
                    new Category { Id = godTheFatherId, Name = "GOD THE FATHER", ParentCategoryId = null, SongBookId = songBookId,TenantId = tenantId, Access = Access.Public },
                    new Category { Id = jesusChristId, Name = "JESUS CHRIST", ParentCategoryId = null, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Id = gospelId, Name = "GOSPEL", ParentCategoryId = null, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Id = christianChurchId, Name = "CHRISTIAN CHURCH", ParentCategoryId = null, SongBookId = songBookId,TenantId = tenantId, Access = Access.Public },
                    new Category { Id = doctrinesId, Name = "DOCTRINES", ParentCategoryId = null, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Id = christianLifeId, Name = "CHRISTIAN LIFE", ParentCategoryId = null, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Id = christianHomeId, Name = "CHRISTIAN HOME", ParentCategoryId = null, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public }
                );
                context.SaveChanges();

                Debug.WriteLine("Categories seeded successfully!");

                context.Categories.AddRange(
                    new Category { Name = "Adoration and Praise", ParentCategoryId = worshipId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Morning Worship", ParentCategoryId = worshipId , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Evening Worship", ParentCategoryId = worshipId , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Opening of Worship", ParentCategoryId = worshipId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Close of Worship", ParentCategoryId = worshipId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category { Name = "TRINITY", ParentCategoryId = null, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category { Name = "Love of God", ParentCategoryId = godTheFatherId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Majesty and Power of God", ParentCategoryId = godTheFatherId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Power of God in Nature", ParentCategoryId = godTheFatherId , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Faithfulness of God", ParentCategoryId = godTheFatherId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Grace and Mercy of God", ParentCategoryId = godTheFatherId , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category { Name = "First Advent", ParentCategoryId = jesusChristId , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Birth", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Life and Ministry", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Sufferings and Death", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Resurrection and Ascension", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Priesthood", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Love Of Christ for Us", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Second Advent", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Kingdom and Reign", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Glory and Praise", ParentCategoryId = jesusChristId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category { Name = "HOLY SPIRIT", ParentCategoryId = null , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category {Name = "Invitation", ParentCategoryId = gospelId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Repentance", ParentCategoryId = gospelId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Forgiveness", ParentCategoryId = gospelId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Consecration", ParentCategoryId = gospelId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Baptism", ParentCategoryId = gospelId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Salvation", ParentCategoryId = gospelId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Redemption", ParentCategoryId = gospelId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category { Name = "CHRISTIAN CHURCH", ParentCategoryId = null , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Community in Christ", ParentCategoryId = christianChurchId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Mission of the Church", ParentCategoryId = christianChurchId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Church Dedication", ParentCategoryId = christianChurchId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Ordination", ParentCategoryId = christianChurchId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Child Dedication", ParentCategoryId = christianChurchId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category {Name = "Sabbath", ParentCategoryId = doctrinesId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Communion", ParentCategoryId = doctrinesId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Law and Grace", ParentCategoryId = doctrinesId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Spiritual Gifts", ParentCategoryId = doctrinesId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Judgement", ParentCategoryId = doctrinesId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Resurrection of the Saints", ParentCategoryId = doctrinesId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Eternal Life", ParentCategoryId = doctrinesId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category { Name = "EARLY ADVENT", ParentCategoryId = null , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category {Name = "Our Love for God", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Joy and Peace", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Hope and Comfort", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Meditation and Prayer", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Faith and Trust", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Guidance", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Thankfulness", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Humility", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Loving Service", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Love for One Another", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Obedience", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Watchfulness", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Christian Warfare", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Pilgrimage", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Stewardship", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Health and Wholeness", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category {Name = "Love of Country", ParentCategoryId = christianLifeId, SongBookId = songBookId, TenantId = tenantId, Access = Access.Public},

                    new Category { Name = "Love in the Home", ParentCategoryId = christianHomeId , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },
                    new Category { Name = "Marriage", ParentCategoryId = christianHomeId , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public },

                    new Category { Name = "SENTENCES AND RESPONSES", ParentCategoryId = null , SongBookId = songBookId, TenantId = tenantId, Access = Access.Public }
                );
                try
                {
                    context.SaveChanges(); // Ensures changes are saved to the database
                    await transaction.CommitAsync();
                    Debug.WriteLine("Categories seeded successfully!");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine("Error Seeding Data: {Error}",ex);
                    return false;
                }
            }
        }
    }


}

