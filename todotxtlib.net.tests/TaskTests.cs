using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace todotxtlib.net.tests
{
    [TestFixture]
    public class TaskTests
    {
        List<string> _projects = new List<string>() { "+test" };
        List<string> _contexts = new List<string>() { "@work" };

        [Test]
        public void Priority_Case()
        {
            // Should have a priority of A
            var task1 = new Task("(A) This is a test task");

            // Doesn't fit the priority rule - should have no priority
            var task2 = new Task("(a) This is a test task");

            // Should have a priority of A
            var task3 = new Task("A", null, null, "This is a test task");

            // Should fix up the priority in the constructor
            var task4 = new Task("a", null, null, "This is a test task");

            Assert.AreEqual(task1.Priority, "A");
            Assert.AreEqual(task3.Priority, "A");
            Assert.AreEqual(task4.Priority, "A");

            Assert.AreEqual(task2.Priority, "");
            Assert.AreEqual(task2.Body, "(a) This is a test task");

            // The setter should fix this up
            task2.Priority = "a";
            Assert.AreEqual(task2.Priority, "A");
            Assert.AreEqual(task2.Body, "(a) This is a test task");
        }

        [Test]
        public void CompletedDate()
        {
            var task1 = new Task("x 2011-03-01 2010-12-31 This task should be completed");

            Assert.IsTrue(task1.CompletedDate != null);
            Assert.AreEqual(new DateTime(2011, 3, 1), task1.CompletedDate);

            var task2 = new Task("2010-12-31 This task should not be completed");

            Assert.IsNull(task2.CompletedDate);

            var task3 = new Task(task1.ToString());
            Assert.IsTrue(task3.CompletedDate != null);
            Assert.AreEqual(new DateTime(2011, 3, 1), task3.CompletedDate);
        }

        #region Create
        [Test]
        public void Create_Priority_Body_Project_Context()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Priority_Body_Context_Project()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Trailing_Whitespace()
        {
            var task = new Task("(A) This is a test task @work +test ");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Null_Priority()
        {
            var task = new Task("This is a test task @work +test ");

            var expectedTask = new Task("", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Priority_In_Body()
        {
            var task = new Task("Oh (A) This is a test task @work +test ");

            var expectedTask = new Task("", _projects, _contexts, "Oh (A) This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Priority_Context_Project_Body()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Completed()
        {
            // Not completed - the completed date is required
            var task = new Task("X (A) This is a test task @work +test ");

            Assert.IsFalse(task.Completed);

            // Completed
            var task2 = new Task("X 2005-06-03 This is a test task @work +test ");

            var expectedTask = new Task("", _projects, _contexts, "This is a test task", null, "", true, new DateTime(2005, 6, 3));
            AssertEquivalence(expectedTask, task2);
        }

        [Test]
        public void Create_UnCompleted()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Multiple_Projects()
        {
            var task = new Task("(A) This is a test task @work +test +test2");

            var expectedTask = new Task("(A)", new List<string>() { "+test", "+test2" }, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Multiple_Contexts()
        {
            var task = new Task("(A) This is a test task @work @home +test");

            var expectedTask = new Task("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_DueDate()
        {
            var task = new Task("(A) This is a test task @work @home +test due:2011-05-08");

            var expectedTask = new Task("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task", null, "2011-05-08", false, null);
            AssertEquivalence(expectedTask, task);
        }

        #endregion

		#region INotifyPropertyChanged Tests

		[Test]
		public void PropertyChanges()
		{
			var task = new Task("A", new List<string> {"+fixsink", "+writenovel"},
			                    new List<string> {"@home", "@work"},
			                    "Write a chapter about fixing the sink");

			task.DueDate = DateTime.Now.AddDays(3).ToString();

			bool fired = false;

			List<String> changedProperties = new List<string>();

			task.PropertyChanged += (sender, e) =>
				{
					fired = true;
					changedProperties.Add(e.PropertyName);
				};

			// Setting task values to their current value shouldn't fire PropertyChanged
			task.Priority = task.Priority;
			task.DueDate = task.DueDate;
			task.Body = task.Body;

			Debug.WriteLine(changedProperties.Aggregate(String.Empty, (l, v) => l + " " + v));

			Assert.False(fired);

			// Setting them to new values should fire PropertyChanged
			task.Priority = "B";
			Debug.WriteLine(changedProperties.Aggregate(String.Empty, (l, v) => l + " " + v));
			Assert.True(fired);
			fired = false;

			task.DueDate = DateTime.Now.AddDays(4).ToString();
			Debug.WriteLine(changedProperties.Aggregate(String.Empty, (l, v) => l + " " + v));
			Assert.True(fired);
			fired = false;

			task.Body = "Delete chapter about the sink";
			Debug.WriteLine(changedProperties.Aggregate(String.Empty, (l, v) => l + " " + v));
			Assert.True(fired);
		}

    	#endregion

		#region ToString

		[Test]
        public void ToString_From_Raw()
        {
            var task = new Task("(A) @work +test This is a test task");
            Assert.AreEqual("(A) @work +test This is a test task", task.ToString());
        }

        [Test]
        public void ToString_From_Parameters()
        {
            var task = new Task("(A)", _projects, _contexts, "This is a test task");
            Assert.AreEqual("(A) This is a test task @work +test", task.ToString());
        }

        #endregion

        static void AssertEquivalence(Task t1, Task t2)
        {
            Assert.AreEqual(t1.Priority, t2.Priority);
            CollectionAssert.AreEquivalent(t1.Projects, t2.Projects);
            CollectionAssert.AreEquivalent(t1.Contexts, t2.Contexts);
            Assert.AreEqual(t1.DueDate, t2.DueDate);
            Assert.AreEqual(t1.Completed, t2.Completed);
            Assert.AreEqual(t1.Body, t2.Body);
        }
    }
}
