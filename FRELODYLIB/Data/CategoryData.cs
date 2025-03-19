using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SongsWithChords.Data.Infrastructure;

namespace SongsWithChords.Models
{
    public class CategoryData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new SongDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<SongDbContext>>(),
                serviceProvider.GetRequiredService<ITenantProvider>()))
            {
                if (context.Categories.Any())
                {
                    Console.WriteLine("Categories Already seeded");
                    return; // DB already been seeded
                }
                Console.WriteLine("Seeding categories...");
                var worshipId = Guid.NewGuid();
                var godTheFatherId = Guid.NewGuid();
                var jesusChristId = Guid.NewGuid();
                var gospelId = Guid.NewGuid();
                var christianChurchId = Guid.NewGuid();
                var doctrinesId = Guid.NewGuid();
                var christianLifeId = Guid.NewGuid();
                var christianHomeId = Guid.NewGuid();

                context.Categories.AddRange(
                    new Category { Name = "WORSHIP", ParentCategoryId = null },
                    new Category { Name = "Adoration and Praise", ParentCategoryId = worshipId },
                    new Category { Name = "Morning Worship", ParentCategoryId = worshipId },
                    new Category { Name = "Evening Worship", ParentCategoryId = worshipId },
                    new Category { Name = "Opening of Worship", ParentCategoryId = worshipId },
                    new Category { Name = "Close of Worship", ParentCategoryId = worshipId },

                    new Category { Name = "TRINITY", ParentCategoryId = null },

                    new Category { Name = "GOD THE FATHER", ParentCategoryId = null },
                    new Category { Name = "Love of God", ParentCategoryId = godTheFatherId },
                    new Category { Name = "Majesty and Power of God", ParentCategoryId = godTheFatherId },
                    new Category { Name = "Power of God in Nature", ParentCategoryId = godTheFatherId },
                    new Category { Name = "Faithfulness of God", ParentCategoryId = godTheFatherId },
                    new Category { Name = "Grace and Mercy of God", ParentCategoryId = godTheFatherId },

                    new Category { Name = "JESUS CHRIST", ParentCategoryId = null },
                    new Category { Name = "First Advent", ParentCategoryId = jesusChristId },
                    new Category { Name = "Birth", ParentCategoryId = jesusChristId },
                    new Category { Name = "Life and Ministry", ParentCategoryId = jesusChristId },
                    new Category { Name = "Sufferings and Death", ParentCategoryId = jesusChristId },
                    new Category { Name = "Resurrection and Ascension", ParentCategoryId = jesusChristId },
                    new Category { Name = "Priesthood", ParentCategoryId = jesusChristId },
                    new Category { Name = "Love Of Christ for Us", ParentCategoryId = jesusChristId },
                    new Category { Name = "Second Advent", ParentCategoryId = jesusChristId },
                    new Category { Name = "Kingdom and Reign", ParentCategoryId = jesusChristId },
                    new Category { Name = "Glory and Praise", ParentCategoryId = jesusChristId },

                    new Category { Name = "HOLY SPIRIT", ParentCategoryId = null },

                    new Category { Name = "GOSPEL", ParentCategoryId = null },
                    new Category { Name = "Invitation", ParentCategoryId = gospelId },
                    new Category { Name = "Repentance", ParentCategoryId = gospelId },
                    new Category { Name = "Forgiveness", ParentCategoryId = gospelId },
                    new Category { Name = "Consecration", ParentCategoryId = gospelId },
                    new Category { Name = "Baptism", ParentCategoryId = gospelId },
                    new Category { Name = "Salvation", ParentCategoryId = gospelId },
                    new Category { Name = "Redemption", ParentCategoryId = gospelId },

                    new Category { Name = "CHRISTIAN CHURCH", ParentCategoryId = null },
                    new Category { Name = "Community in Christ", ParentCategoryId = christianChurchId },
                    new Category { Name = "Mission of the Church", ParentCategoryId = christianChurchId },
                    new Category { Name = "Church Dedication", ParentCategoryId = christianChurchId },
                    new Category { Name = "Ordination", ParentCategoryId = christianChurchId },
                    new Category { Name = "Child Dedication", ParentCategoryId = christianChurchId },

                    new Category { Name = "DOCTRINES", ParentCategoryId = null },
                    new Category { Name = "Sabbath", ParentCategoryId = doctrinesId },
                    new Category { Name = "Communion", ParentCategoryId = doctrinesId },
                    new Category { Name = "Law and Grace", ParentCategoryId = doctrinesId },
                    new Category { Name = "Spiritual Gifts", ParentCategoryId = doctrinesId },
                    new Category { Name = "Judgement", ParentCategoryId = doctrinesId },
                    new Category { Name = "Resurrection of the Saints", ParentCategoryId = doctrinesId },
                    new Category { Name = "Eternal Life", ParentCategoryId = doctrinesId },

                    new Category { Name = "EARLY ADVENT", ParentCategoryId = null },

                    new Category { Name = "CHRISTIAN LIFE", ParentCategoryId = null },
                    new Category { Name = "Our Love for God", ParentCategoryId = christianLifeId },
                    new Category { Name = "Joy and Peace", ParentCategoryId = christianLifeId },
                    new Category { Name = "Hope and Comfort", ParentCategoryId = christianLifeId },
                    new Category { Name = "Meditation and Prayer", ParentCategoryId = christianLifeId },
                    new Category { Name = "Faith and Trust", ParentCategoryId = christianLifeId },
                    new Category { Name = "Guidance", ParentCategoryId = christianLifeId },
                    new Category { Name = "Thankfulness", ParentCategoryId = christianLifeId },
                    new Category { Name = "Humility", ParentCategoryId = christianLifeId },
                    new Category { Name = "Loving Service", ParentCategoryId = christianLifeId },
                    new Category { Name = "Love for One Another", ParentCategoryId = christianLifeId },
                    new Category { Name = "Obedience", ParentCategoryId = christianLifeId },
                    new Category { Name = "Watchfulness", ParentCategoryId = christianLifeId },
                    new Category { Name = "Christian Warfare", ParentCategoryId = christianLifeId },
                    new Category { Name = "Pilgrimage", ParentCategoryId = christianLifeId },
                    new Category { Name = "Stewardship", ParentCategoryId = christianLifeId },
                    new Category { Name = "Health and Wholeness", ParentCategoryId = christianLifeId },
                    new Category { Name = "Love of Country", ParentCategoryId = christianLifeId },

                    new Category { Name = "CHRISTIAN HOME", ParentCategoryId = null },
                    new Category { Name = "Love in the Home", ParentCategoryId = christianHomeId },
                    new Category { Name = "Marriage", ParentCategoryId = christianHomeId },

                    new Category { Name = "SENTENCES AND RESPONSES", ParentCategoryId = null }
                );
                try
                {
                    context.SaveChanges(); // Ensures changes are saved to the database					
                    Console.WriteLine("Categories seeded successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Seeding Data: {ex.Message}");
                    throw;
                }
            }
        }
    }


}

