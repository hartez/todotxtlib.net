using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace todotxtlib.net.tests
{
    public class TaskTests
    {
        List<string> _projects = new List<string>() { "+test" };
        List<string> _contexts = new List<string>() { "@work" };

        [Fact]
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

            Assert.Equal(task1.Priority, "A");
            Assert.Equal(task3.Priority, "A");
            Assert.Equal(task4.Priority, "A");

            Assert.Equal(task2.Priority, "");
            Assert.Equal(task2.Body, "(a) This is a test task");

            // The setter should fix this up
            task2.Priority = "a";
            Assert.Equal(task2.Priority, "A");
            Assert.Equal(task2.Body, "(a) This is a test task");
        }

        [Fact]
        public void CompletedDate()
        {
            var task1 = new Task("x 2011-03-01 2010-12-31 This task should be completed");

            Assert.True(task1.CompletedDate != null);
            Assert.Equal(new DateTime(2011, 3, 1), task1.CompletedDate);

            var task2 = new Task("2010-12-31 This task should not be completed");

            Assert.Null(task2.CompletedDate);

            var task3 = new Task(task1.ToString());
            Assert.True(task3.CompletedDate != null);
            Assert.Equal(new DateTime(2011, 3, 1), task3.CompletedDate);
        }

        #region Create
        [Fact]
        public void Create_Priority_Body_Project_Context()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_Priority_Body_Context_Project()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_Trailing_Whitespace()
        {
            var task = new Task("(A) This is a test task @work +test ");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_Null_Priority()
        {
            var task = new Task("This is a test task @work +test ");

            var expectedTask = new Task("", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_Priority_In_Body()
        {
            var task = new Task("Oh (A) This is a test task @work +test ");

            var expectedTask = new Task("", _projects, _contexts, "Oh (A) This is a test task");
            AssertEquivalence(expectedTask, task);
        }

		[Fact]
		public void Create_Project_In_Body()
		{
			var task = new Task("Oh (A) This is a test task @work +test ");

			Assert.True(task.Projects.Contains("+test"));
		}

        [Fact]
        public void Create_Priority_Context_Project_Body()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_Completed()
        {
            // Not completed - the completed date is required
            var task = new Task("X (A) This is a test task @work +test ");

            Assert.False(task.Completed);

            // Completed
            var task2 = new Task("X 2005-06-03 This is a test task @work +test ");

            var expectedTask = new Task("", _projects, _contexts, "This is a test task", null, "", true, new DateTime(2005, 6, 3));
            AssertEquivalence(expectedTask, task2);
        }

        [Fact]
        public void Create_UnCompleted()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_Multiple_Projects()
        {
            var task = new Task("(A) This is a test task @work +test +test2");

            var expectedTask = new Task("(A)", new List<string>() { "+test", "+test2" }, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_Multiple_Contexts()
        {
            var task = new Task("(A) This is a test task @work @home +test");

            var expectedTask = new Task("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Fact]
        public void Create_DueDate()
        {
            var task = new Task("(A) This is a test task @work @home +test due:2011-05-08");

            var expectedTask = new Task("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task", null, "2011-05-08", false, null);
            AssertEquivalence(expectedTask, task);
        }

        #endregion

		#region INotifyPropertyChanged Tests

		[Fact]
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

		[Fact]
		public void BodyOnly()
		{
			var task = new Task("test");

			Assert.NotEmpty(task.Body);
			Assert.Equal("test", task.Body);

			task.Body = "test2";
			Assert.NotEmpty(task.Body);
			Assert.Equal("test2", task.Body);
		}

    	#region ToString

		[Fact]
        public void ToString_From_Raw()
        {
            var task = new Task("(A) @work +test This is a test task");
            Assert.Equal("(A) @work +test This is a test task", task.ToString());
        }

        [Fact]
        public void ToString_From_Parameters()
        {
            var task = new Task("(A)", _projects, _contexts, "This is a test task");
            Assert.Equal("(A) This is a test task @work +test", task.ToString());
        }

        #endregion

		[Fact]
		public void Equality()
		{
			var rawString = "This is a task @online @home +project +anotherproject";
			Task a = new Task(rawString);
			Task b = new Task(rawString);
			Task c = new Task("This is different task @home +anotherproject");

			IEqualityComparer<Task> comparer = new TaskEqualityComparer();

			Assert.True(comparer.Equals(a, b));
			Assert.False(comparer.Equals(b, c));
			Assert.False(comparer.Equals(a, c));
		}

    	static void AssertEquivalence(Task t1, Task t2)
        {
            Assert.Equal(t1.Priority, t2.Priority);
            Assert.Equivalent(t1.Projects, t2.Projects);
            Assert.Equivalent(t1.Contexts, t2.Contexts);
            Assert.Equal(t1.DueDate, t2.DueDate);
            Assert.Equal(t1.Completed, t2.Completed);
            Assert.Equal(t1.Body, t2.Body);
        }

		[Fact]
		public void RecognizePhoneNumber()
		{
			var task = new Task("This task contains the phone number 720-564-1231");

			Assert.True(task.Metadata["phone"] == "720-564-1231"); 
		}

		[Fact]
		public void ExplicitPhoneNumber()
		{
			var task = new Task("This task contains an explicit phone number phone:720-564-1231");

			Assert.True(task.Metadata["phone"] == "720-564-1231"); 
		}

		[Fact]
		public void MultipleRecognizePhoneNumber()
		{
			var task = new Task("This task contains the phone number (720) 564-1231 and the number (317)228-1231");

			Assert.True(task.Metadata.ContainsKey("phone"), "Metadata should have key 'phone'");
			Assert.True(task.Metadata.ContainsKey("phone1"), "Metadata should have key 'phone1'");

			Assert.True(task.Metadata["phone"] == "(720) 564-1231", "'phone' should be (720) 564-1231");
			Assert.True(task.Metadata["phone1"] == "(317)228-1231", "'phone1' should be (317)228-1231");
		}

		[Fact]
		public void MultipleExplicitPhoneNumber()
		{
			var task = new Task("This task contains an explicit phone:720-564-1231 and another phone:317.228.1231");

			Assert.True(task.Metadata["phone"] == "720-564-1231", "'phone' should have been 720-564-1231");
			Assert.True(task.Metadata["phone1"] == "317.228.1231", "'phone1' should have been 317.228.1231");
		}

		[Fact]
		public void MultipleMixedPhoneNumber()
		{
			var task = new Task("This task contains an explicit phone:720-564-1231 and another 317-228-1231");

			Assert.True(task.Metadata["phone"] == "720-564-1231");
			Assert.True(task.Metadata["phone1"] == "317-228-1231");

			// Different order; explicit metadata should still be first
			task = new Task("This task contains a number 720-564-1231 and another phone:317-228-1231 ");

			Assert.True(task.Metadata["phone"] == "317-228-1231");
			Assert.True(task.Metadata["phone1"] == "720-564-1231");
		}

        [Fact]
        public void ShouldAllowProjectWithHyphen()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = new Task("This task contains a project with a hypen +hyphen-project @home");

            Assert.True(task.Projects.Contains("+hyphen-project"));
        }

        [Fact]
        public void ShouldAllowContextWithHyphen()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = new Task("This task contains a project with a hypen @hyphen-context @home");

            Assert.True(task.Contexts.Contains("@hyphen-context"));
        }

        [Fact]
        public void ProjectMustEndWithAlphanumericOrUnderscore()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = new Task("This task has only one project with a hyphen in it +hyphen-project +nohyphen-");

            Assert.True(task.Projects.Contains("+hyphen-project"));
            Assert.False(task.Projects.Contains("+nohyphen-"));
            Assert.True(task.Projects.Contains("+nohyphen"));
        }

        [Fact]
        public void ContextMustEndWithAlphanumericOrUnderscore()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = new Task("This task has only one valid context @hyphen-context @nohyphen-");

            Assert.True(task.Contexts.Contains("@hyphen-context"));
            Assert.DoesNotContain("@nohyphen-", task.Contexts);
            Assert.True(task.Contexts.Contains("@nohyphen"));
        }

        [Fact]
        public void MetaDataMustNotHaveWhitespaceAfterColon()
        {
            var task =
                new Task(
                    "due:2014-07-02 Get back to work on project x Ansel 453.456.8967  Jim 432.453.9134  Bob's cell: 812.477.7272");

            Assert.True(task.DueDate == "2014-07-02");
            Assert.False(task.Metadata.ContainsKey("cell"), "metadata cannot have whitespace");
        }

        [Fact]
        public void MultiplePhoneNumbersShouldNotConflict()
        {
            var task =
                new Task(
                    "due:2014-07-02 Get back to work on project x Ansel 453.456.8967  Jim 432.453.9134  Bob's cell: 812.477.7272");

            Assert.True(task.Metadata["phone"] == "453.456.8967");
            Assert.True(task.Metadata["phone1"] == "432.453.9134");
            Assert.True(task.Metadata["phone2"] == "812.477.7272");
        }
    }
}
