using System.Collections.Generic;
using twitter.Api;

namespace twitter.Services
{
    public interface ITweetProvider
    {
        IEnumerable<DB.Tweet> Run();
        void Stop();
    }
}