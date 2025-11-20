using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOCAPI.Modules.Zdx
{
    public static class MetricsCache
    {

        private static readonly object _lock = new();
        private static string _metrics = "# No data yet";

        public static void Update(string metrics)
        {
            lock (_lock)
            {
                _metrics = metrics;
            }
        }

        public static string Get() => _metrics;

    }
}
