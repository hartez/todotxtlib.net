using System;

namespace todotxtlib.net
{
    public class TaskException : Exception
    {

        public TaskException(string message)
            : base(message)
        {

        }
        public TaskException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }
}