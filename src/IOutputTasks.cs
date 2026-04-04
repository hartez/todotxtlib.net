using System.Collections.Generic;

namespace todotxtlib.net
{
    public interface IOutputTasks
    {
        public IEnumerable<string> ToOutput();
        public IEnumerable<string> ToNumberedOutput();
    }
}