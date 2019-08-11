using System;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace twitter.DB
{
    public class Tweet
    {
        public long ID {get;set;}
        public Point Location {get;set;}
        public string Data { get; set; }
        public DateTime AddedTime { get; set; } 
    }
}