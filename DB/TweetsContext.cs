using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace twitter.DB
{
    public class TweetsContext:DbContext
    {
        public DbSet<Tweet> Tweets { get; set; }
        public TweetsContext()
        {

        }
        public TweetsContext(DbContextOptions<TweetsContext> options)
            : base(options)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder builder)
        //{
        //    if (!builder.IsConfigured)
        //    {
        //        builder.UseNpgsql("Host=localhost;Database=twitDB;Username=postgres;Password=lenblenb7",
        //            o => o.UseNetTopologySuite());
        //    }
        //}

        public async Task<EntityEntry<Tweet>> AddTweetWithCheckAsync(Tweet tweet)
        {
            EntityEntry<Tweet> tweetEntity = null;
            try
            {
                tweetEntity = await Tweets.AddAsync(tweet);
                await SaveChangesAsync(); // Will throw if tweet already exists
            }
            catch (DbUpdateException)
            {
                if (tweetEntity != null)
                {
                    tweetEntity.State = EntityState.Detached;
                    tweetEntity = null;
                }
            }   
            return tweetEntity;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("postgis");
            modelBuilder.Entity<Tweet>(entity =>
            {
                entity.ToTable("Twits");
                entity.HasKey(m => m.ID);
                entity.Property(m => m.Location)
                      .HasColumnType("geography (point)");
                entity.Property(m => m.Data)
                      .HasColumnType("jsonb");

                entity.ForNpgsqlHasIndex(m => m.Location);
            });
        }
    }
}