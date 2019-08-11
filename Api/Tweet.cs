using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace twitter.Api
{
    public class Tweet
    {
        public long ID { get; set; }
        public Location Location { get; set; }
        public JObject Data { get; set; }
    }
}
