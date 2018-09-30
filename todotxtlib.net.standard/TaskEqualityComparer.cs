using System.Collections.Generic;

namespace todotxtlib.net
{
	public class TaskEqualityComparer : IEqualityComparer<Task>
	{
		#region IEqualityComparer<Task> Members

		public bool Equals(Task x, Task y)
		{
			return x.ToString() == y.ToString();
		}

		public int GetHashCode(Task obj)
		{
			return obj.ToString().GetHashCode();
		}

		#endregion
	}
}