using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace twitter.Services
{
    public class TweetLoadingService : IHostedService
    {
        private readonly ITweetProvider tweetProvider;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<TweetLoadingService> logger;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private Task workerTask;

        public TweetLoadingService(ITweetProvider tweetProvider, IServiceScopeFactory serviceScopeFactory, ILogger<TweetLoadingService> logger)
        {
            this.tweetProvider = tweetProvider;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation(nameof(TweetLoadingService) + " started.");
            workerTask = Task.Run(()=>RunInBackgroundAsync(cancellation.Token));
            if (workerTask.IsCompleted)
                return workerTask;
            return Task.CompletedTask;
        }

        private async Task RunInBackgroundAsync(CancellationToken cancellationToken)
        {
            using(var scope = serviceScopeFactory.CreateScope())
            {
                var tweetsDB = scope.ServiceProvider.GetRequiredService<DB.TweetsContext>();
                foreach (var tweet in tweetProvider.Run())
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    await HandleTweet(tweetsDB, tweet);
                }
            }
        }

        private async Task HandleTweet(DB.TweetsContext tweetsDB, DB.Tweet tweet)
        {
            try
            {
                var t = await tweetsDB.AddTweetWithCheckAsync(tweet);
                if (t == null)
                {
                    logger.LogDebug($"Duplicate - id: {tweet.ID}");
                    return;
                }
                logger.LogDebug($"Tweet added - id: {t.Entity.ID} @{t.Entity.Location}");
                
            }
            catch(Exception e)
            {
                logger.LogError(e,"Error during background tweet handling");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (workerTask == null)
                return;
            try
            {
                tweetProvider.Stop();
                cancellation.Cancel();
            }
            finally
            {
                await Task.WhenAny(workerTask, Task.Delay(Timeout.Infinite, cancellationToken));
                logger.LogInformation(nameof(TweetLoadingService) + " stoped.");
            }
        }

        public void Dispose()
        {
            cancellation.Cancel();
        }
    }
}
