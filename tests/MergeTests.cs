using System;
using System.Linq;
using Xunit;

namespace todotxtlib.net.tests
{
    public class MergeTests : IDisposable
    {
        private string _originalPath = "merge0.txt";
        private string _location1Path = "merge1.txt";
        private string _location2Path = "merge2.txt";

        private TaskList _mergeResult;

        public MergeTests()
        {
            var t0 = new TaskList(_originalPath);

            var t1 = new TaskList(_location1Path);

            var t2 = new TaskList(_location2Path);

            _mergeResult = TaskList.Merge(t0, t1, t2);
        }

        [Fact]
        public void Priority_Change()
        {
            var checkupTask = _mergeResult.Search("checkup").First();

            Assert.Equal("D", checkupTask.Priority);
        }

        [Fact]
        public void Line_Removed()
        {
            var task = _mergeResult.Search("milk").FirstOrDefault();

            Assert.Null(task);
        }

        [Fact]
        public void Line_Multiple_Changes()
        {
            var task = _mergeResult.Search("herb").FirstOrDefault();

            Assert.NotNull(task);
            Assert.Contains("Plant", task.ToString());
            Assert.Contains("vegetable", task.ToString());
        }

        [Fact]
        public void Conflict_Last_In_Wins()
        {
            var task = _mergeResult.Search("mobile").FirstOrDefault();

            Assert.NotNull(task);
            Assert.False(task.Completed); // Was complete in original and merge1, not complete in merge2
        }

        [Fact]
        public void Contains_Tasks_Added_In_Both()
        {
            var task1 = _mergeResult.Search("Star").FirstOrDefault();
            var task2 = _mergeResult.Search("videos").FirstOrDefault();

            Assert.NotNull(task1);
            Assert.NotNull(task2);
        }

        public void Dispose()
        {
            
        }
    }
}