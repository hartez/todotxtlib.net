using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace todotxtlib.net.tests
{
    [TestFixture]
    internal class MergeTests
    {
        private string _originalPath = "merge0.txt";
        private string _location1Path = "merge1.txt";
        private string _location2Path = "merge2.txt";

        private TaskList _mergeResult;

        [SetUp]
        public void Setup()
        {
            var t0 = new TaskList(_originalPath);

            var t1 = new TaskList(_location1Path);

            var t2 = new TaskList(_location2Path);

            _mergeResult = TaskList.Merge(t0, t1, t2);
        }

        [Test]
        public void Priority_Change()
        {
            var checkupTask = _mergeResult.Search("checkup").First();

            Assert.That(checkupTask.Priority == "D");
        }

        [Test]
        public void Line_Removed()
        {
            var task = _mergeResult.Search("milk").FirstOrDefault();

            Assert.IsNull(task);
        }

        [Test]
        public void Line_Multiple_Changes()
        {
            var task = _mergeResult.Search("herb").FirstOrDefault();

            Assert.IsNotNull(task);
            Assert.That(task.ToString().Contains("Plant"));
            Assert.That(task.ToString().Contains("vegetable"));
        }

        [Test]
        public void Conflict_Last_In_Wins()
        {
            var task = _mergeResult.Search("mobile").FirstOrDefault();

            Assert.IsNotNull(task);
            Assert.That(task.Completed == false); // Was complete in original and merge1, not complete in merge2
        }

        [Test]
        public void Contains_Tasks_Added_In_Both()
        {
            var task1 = _mergeResult.Search("Star").FirstOrDefault();
            var task2 = _mergeResult.Search("videos").FirstOrDefault();

            Assert.IsNotNull(task1);
            Assert.IsNotNull(task2);
        }
    }
}