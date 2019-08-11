using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Tweetinvi;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using twitter.Controllers;

namespace twitter.Services
{
    public class TweetProvider : ITweetProvider
    {
        private readonly ITwitterCredentials credentials;
        private readonly bool useFakeLocation;
        private BlockingCollection<DB.Tweet> tweets;
        private ISampleStream stream;

        public TweetProvider(string consumerKey, string consumerSecret, string accessToken,
                            string accessTokenSecret, bool useFakeLocation=false)
        {
            this.credentials = new TwitterCredentials(consumerKey, consumerSecret, accessToken,
                                                      accessTokenSecret);
            this.useFakeLocation = useFakeLocation;
        }


        public IEnumerable<DB.Tweet> Run()
        {
            tweets = new BlockingCollection<DB.Tweet>();
            stream = Stream.CreateSampleStream(credentials);
            stream.TweetReceived += (s,e)=> AddTweet(e);
            stream.StartStreamAsync();
            return tweets.GetConsumingEnumerable();
        }

        public void Stop()
        {
            stream.StopStream();
            tweets.CompleteAdding();
        }

        private void AddTweet(TweetReceivedEventArgs e)
        {
            var location = GetLocation(e.Tweet.Coordinates);
            if (location == null)
                return;

            var tweet = new DB.Tweet
            {
                ID = e.Tweet.Id,
                Location = location as Point,
                Data = e.Json
            };
            
            tweets.Add(tweet);
        }

        Random rand = new Random();
        private IPoint RandomLocation()
        {
            double latitude = rand.NextDouble() * 90.0;
            double longitude = rand.NextDouble() * 360.0 - 180.0;
            return (longitude, latitude).ToPoint();
        }

        private IPoint GetLocation(ICoordinates coords)
        {
            if(useFakeLocation)
                return RandomLocation();
            if (coords == null)
                return null;
            return (coords.Longitude, coords.Latitude).ToPoint();
        }
    }
}
