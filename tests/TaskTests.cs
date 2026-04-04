using System;
using Xunit;

namespace todotxtlib.net.tests
{
    public class TaskTests
    {
        [Fact]
        public void Priority_Case()
        {
            // Should have a priority of A
            var task1 = Task.Parse("(A) This is a test task");

            // Doesn't fit the priority rule - should have no priority
            var task2 = Task.Parse("(a) This is a test task");

            Assert.Equal('A', task1.Priority);
            
            Assert.Null(task2.Priority);
            Assert.Equal("(a) This is a test task", task2.Body);
        }

        [Fact]
        public void CompletedDate()
        {
            var task1 = Task.Parse("x 2011-03-01 2010-12-31 This task should be completed");

            Assert.NotNull(task1.CompletedDate);
            Assert.Equal(new DateTime(2011, 3, 1), task1.CompletedDate);

            var task2 = Task.Parse("2010-12-31 This task should not be completed");

            Assert.Null(task2.CompletedDate);

            var task3 = Task.Parse(task1.ToString());
            Assert.NotNull(task3.CompletedDate);
            Assert.Equal(new DateTime(2011, 3, 1), task3.CompletedDate);
        }

        #region Create
        [Fact]
        public void Constructor_Ignores_Trailing_Whitespace()
        {
            var task = Task.Parse("(A) This is a test task @work +test  ");

            var expectedTask = Task.Parse("(A) This is a test task @work +test");
            Assert.Equal(expectedTask, task);
        }

        [Fact]
        public void Create_Null_Priority()
        {
            var task = Task.Parse("This is a test task @work +test ");

            Assert.Null(task.Priority);
        }

        [Fact]
        public void Priority_Must_Be_First()
        {
            var task = Task.Parse("Oh (A) This is a test task @work +test ");
            Assert.Null(task.Priority);
        }

		

        [Fact]
        public void Completed_Task()
        {
            var task = Task.Parse("x 2026-04-02 This task is completed");
            Assert.True(task.Completed);
        }

        [Fact]
        public void Completed_Must_Begin_With_x()
        {
            var task = Task.Parse("X 2026-04-02 This task is not completed");
            Assert.False(task.Completed);
        }

        [Fact]
        public void Completed_Must_Include_Completed_Date()
        {
            var task = Task.Parse("x This task is not completed");
            Assert.False(task.Completed);
        }

        [Fact]
		public void Project_And_Context()
		{
			var task = Task.Parse("This is a test task @work +test ");
			Assert.Contains("+test", task.Projects);
            Assert.Contains("@work", task.Contexts);

            Assert.DoesNotContain("+test", task.Contexts);
            Assert.DoesNotContain("@work", task.Projects);
		}


        [Fact]
        public void Multiple_Projects()
        {
            var task = Task.Parse("(A) This is a test task @work +test +test2");

            Assert.Contains("+test", task.Projects);
            Assert.Contains("+test2", task.Projects);
        }

        [Fact]
        public void Multiple_Contexts()
        {
            var task = Task.Parse("(A) This is a test task @work @home +test");

            Assert.Contains("@work", task.Contexts);
            Assert.Contains("@home", task.Contexts);
        }

        [Fact]
        public void DueDate()
        {
            var task = Task.Parse("(A) This is a test task @work @home +test due:2011-05-08");
            Assert.Equal("2011-05-08", task.DueDate);
        }

        #endregion

		[Fact]
		public void BodyOnly()
		{
			var task = Task.Parse("test");

			Assert.NotEmpty(task.Body);
			Assert.Equal("test", task.Body);
		}

    	#region ToString

		[Fact]
        public void ToString_Matches_Input_String()
        {
            var expected = "(A) @work +test This is a test task";

            var task = Task.Parse(expected);
            Assert.Equal(expected, task.ToString());
        }

        [Fact]
        public void ToString_Matches_Input_String_With_DueDate()
        {
            var expected = "(A) @work +test This is a test task due:2011-07-08";

            var task = Task.Parse(expected);
            Assert.Equal(expected, task.ToString());
        }

        [Fact]
        public void ToString_Matches_Input_String_With_Contexts_And_Projects()
        {
            var expected = "(A) @work +test This is a @context test +project task";

            var task = Task.Parse(expected);
            Assert.Equal(expected, task.ToString());
        }

        [Fact]
        public void Empty_Task_Outputs_Blank_Line()
        {
            Assert.Equal("", Task.Empty.ToString());
        }

        #endregion

		[Fact]
		public void Equality()
		{
			var rawString = "This is a task @online @home +project +anotherproject";
			Task a = Task.Parse(rawString);
			Task b = Task.Parse(rawString);
			Task c = Task.Parse("This is different task @home +anotherproject");

			Assert.Equal(a, b);
			Assert.NotEqual(b, c);
			Assert.NotEqual(a, c);
		}

        [Fact]
        public void ShouldAllowProjectWithHyphen()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = Task.Parse("This task contains a project with a hypen +hyphen-project @home");

            Assert.Contains("+hyphen-project", task.Projects);
        }

        [Fact]
        public void ShouldAllowContextWithHyphen()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = Task.Parse("This task contains a project with a hypen @hyphen-context @home");

            Assert.Contains("@hyphen-context", task.Contexts);
        }

        [Fact]
        public void ProjectMustEndWithAlphanumericOrUnderscore()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = Task.Parse("This task has only one project with a hyphen in it +hyphen-project +nohyphen-");

            Assert.Contains("+hyphen-project", task.Projects);
            Assert.DoesNotContain("+nohyphen-", task.Projects);
            Assert.Contains("+nohyphen", task.Projects);
        }

        [Fact]
        public void ContextMustEndWithAlphanumericOrUnderscore()
        {
            // "A project or context contains any non-whitespace character and must end in an alphanumeric or ‘_’. "

            var task = Task.Parse("This task has only one valid context @hyphen-context @nohyphen-");

            Assert.Contains("@hyphen-context", task.Contexts);
            Assert.DoesNotContain("@nohyphen-", task.Contexts);
            Assert.Contains("@nohyphen", task.Contexts);
        }

        [Fact]
        public void MetaDataMustNotHaveWhitespaceAfterColon()
        {
            var task =
                Task.Parse(
                    "due:2014-07-02 Get back to work on project x Ansel 453.456.8967  Jim 432.453.9134  Bob's cell: 812.477.7272");

            Assert.True(task.DueDate == "2014-07-02");
            Assert.False(task.Metadata.ContainsKey("cell"), "metadata cannot have whitespace");
        }
    }
}
