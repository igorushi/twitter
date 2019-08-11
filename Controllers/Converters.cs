using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Linq;
using System;

namespace twitter.Controllers
{
    public static class Converters
    {
        private static IGeometryFactory geometryFactory
            = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public static IPoint ToPoint(this (double, double) coords)
        {
            return geometryFactory.CreatePoint(new Coordinate(coords.Item1, coords.Item2));
        }

        public static Api.Location ToLocation(this IPoint p)
        {
            return new Api.Location
            {
                Longitude = p.X,
                Latitude = p.Y
            };
        }

        public static Api.Tweet ToApi(this DB.Tweet tweet)
        {
            return new Api.Tweet
            {
                ID = tweet.ID,
                Location = tweet.Location.ToLocation(),
                Data = JObject.Parse(tweet.Data)
            };
        }
    }
}
