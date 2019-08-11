using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace twitter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TweetsController : ControllerBase
    {
        private readonly DB.TweetsContext _context;
        public TweetsController(DB.TweetsContext context)
        {
            _context = context;
        }

        // GET: api/Tweets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Api.Tweet>>> GetTweets(
            [FromQuery] double longtitude = 0,
            [FromQuery] double latitude = 0, 
            [FromQuery] double radiusMeter = 100_000/*100km*/)
        {
            if (!CheckRange(radiusMeter, 0, 40_075_000))
                return BadRequest("radiusMeter is out of accepted range (0, 40'075'000)");

            if (!CheckRange(latitude, -90, 90))
                return BadRequest("latitude is out of accepted range (0, 90)");

            if (!CheckRange(longtitude, -180, 180))
                return BadRequest("longtitude is out of accepted range (-180, 180)");

            IPoint point = (longtitude, latitude).ToPoint();
            var tweets = await _context.Tweets
                            .Where(t => t.Location.Distance(point) < radiusMeter)
                            .ToListAsync();
            return Ok(tweets.Select(t=>t.ToApi()));
        }

        private bool CheckRange(double parameter, double low, int high)
        {
            return parameter >= low && parameter <= high;
        }
    }
}
