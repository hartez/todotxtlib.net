using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace todotxtlib.net.tests
{
    public class TaskListTests
    {
        private const string _testDataPath = "testtasks.txt";

        private string CreateTempTasksFile()
        {
            string tempTaskFile = Path.GetRandomFileName();
            File.Copy(_testDataPath, tempTaskFile, true);
            return tempTaskFile;
        }

        [Fact]
        public void AggregateContexts()
        {
            string tempTaskFile = CreateTempTasksFile();
            var taskList = new TaskList(tempTaskFile);
            IOrderedEnumerable<string> contexts = taskList.SelectMany(numberedTask => numberedTask.Task.Contexts,
                                                                (task, context) => context).Distinct().OrderBy(context => context);

            Assert.Equal(3, contexts.Count());
        }

        [Fact]
        public void Add_Multiple()
        {
            var taskList = new TaskList(_testDataPath);
            int c = taskList.Count;

            taskList.Add("Add_Multiple task one");
            taskList.Add("Add_Multiple task two");

            Assert.Equal(c + 2, taskList.Count);
        }

        [Fact]
        public void Add_ToFile()
        {
            // Create a copy of test data so we can leave the original alone
            string tempTaskFile = CreateTempTasksFile();

            List<string> fileContents = [.. File.ReadAllLines(tempTaskFile)];
            fileContents.Add("(B) Add_ToFile +test @task");

            var tl = new TaskList(tempTaskFile);
            tl.Add(fileContents.Last());
            tl.Save(tempTaskFile);

            string[] newFileContents = File.ReadAllLines(tempTaskFile);

            Assert.Equivalent(fileContents, newFileContents);

            // Clean up
            File.Delete(tempTaskFile);
        }

        [Fact]
        public void BlankLinesAreEmptyTasks()
        {
            // Create a copy of test data so we can leave the original alone
            string tempTaskFile = CreateTempTasksFile();

            var tl = new TaskList(tempTaskFile);
            var originalCount = tl.Count;

            File.AppendAllText(tempTaskFile, Environment.NewLine + Environment.NewLine + "The above line was blank" + Environment.NewLine);

            var st = File.Open(tempTaskFile, FileMode.Open);

            st.Close();

            var tl2 = new TaskList(tempTaskFile);

            Assert.Equal(originalCount + 2, tl2.Count); // "Added two lines, one of which was blank (empty)"
        }

        [Fact]
        public void Add_To_Empty_File()
        {
            // v0.3 and earlier contained a bug where a blank task was added
            string tempTaskFile = CreateTempTasksFile();
            File.WriteAllLines(tempTaskFile, new string[] { }); // empties the file

            var tl = new TaskList(tempTaskFile)
            {
                "A task"
            };

            Assert.Single(tl);

            // Clean up
            File.Delete(tempTaskFile);
        }

        [Fact]
        public void Construct()
        {
            _ = new TaskList(_testDataPath);
        }

        [Fact]
        public void Delete_InFile()
        {
            string tempTasksFile = CreateTempTasksFile();
            try
            {
                string[] fileLines = File.ReadAllLines(tempTasksFile);
                List<string> fileContents = [.. fileLines];
                var task = Task.Parse(fileContents.Last());
                fileContents.Remove(fileContents.Last());

                var tl = new TaskList(tempTasksFile);
                tl.RemoveTask(tl.Last().Number);
                tl.Save(tempTasksFile);

                string[] newFileContents = File.ReadAllLines(tempTasksFile);
                Assert.Equivalent(fileContents, newFileContents);
            }
            finally
            {
                File.Delete(tempTasksFile);
            }
        }

        [Fact]
        public void Load_From_File()
        {
            var tl = new TaskList(_testDataPath);
            Assert.Equal(8, tl.Count);
            Assert.Equal("A task", tl.GetTask(1).Body);
        }

        [Fact]
        public void Search()
        {
            string tempTasksFile = CreateTempTasksFile();
            var tl = new TaskList(tempTasksFile);

            // There should be two tasks which contain the term 'foo'
            var fooTaskList = tl.Search("foo");
            Assert.NotNull(fooTaskList);
            Assert.Equal(2, fooTaskList.Count());

            // Search should be case insenstive
            var caseInsensitiveTaskList = tl.Search("Foo");
            Assert.NotNull(caseInsensitiveTaskList);
            Assert.Equal(2, caseInsensitiveTaskList.Count());

            // '-' in front of the term should find all tasks without the term
            var notFooTaskList = tl.Search("-foo");
            
            // So searching the list generated by the negative search for the term
            // should give us an empty list
            Assert.DoesNotContain(notFooTaskList, nt => nt.Task.Body.Contains("foo"));
        }

        [Fact]
        public void ToggleComplete_Off_InCollection()
        {
            // Not complete - doesn't include completed date
            var taskList = new TaskList(_testDataPath)
            {
                "X (B) ToggleComplete_Off_InCollection +test @task"
            };

            var itemNumber = taskList.Count;

            taskList.ToggleCompleted(itemNumber);

            Assert.True(taskList.Last().Completed);

            taskList.Add("x 2011-02-25 ToggleComplete_Off_InCollection +test @task");

            itemNumber = taskList.Count;

            taskList.ToggleCompleted(itemNumber);

            Assert.False(taskList.Last().Completed);
        }

        [Fact]
        public void ToggleComplete_On_InCollection()
        {
            var taskList = new TaskList(_testDataPath)
            {
                "(B ToggleComplete_On_InCollection +test @task"
            };

            taskList.ToggleCompleted(taskList.Count);

            Assert.True(taskList.Last().Completed);
        }

        [Fact]
        public void UpdateTaskPriority()
        {
            var text = @"this is the first task
this is the second task
";

            var tl = new TaskList
            {
                text
            };

            Assert.Null(tl.GetTask(1).Priority);

            tl.SetItemPriority(1, 'a');
            Assert.Equal('A', tl.GetTask(1).Priority);
        }

        [Fact]
        public void MarkCompleteSetsCompletedDate()
        {
            var taskList = new TaskList(_testDataPath);
            taskList.MarkCompleted(8);
            var task = taskList.GetTask(8);

            Assert.True(task.Completed);
            Assert.NotNull(task.CompletedDate);
        }

        [Fact]
        public void AddDoNotEnsureCreatedDate()
        {
            var hasCreatedDate = "2025-04-03 Has a created date";
            var noCreatedDate = "Has no created date";
         
            var taskList = new TaskList();

            taskList.Create(hasCreatedDate, ensureCreatedDate: false);
            taskList.Create(noCreatedDate, ensureCreatedDate: false);
            
            Assert.NotNull(taskList.GetTask(1).CreatedDate);
            Assert.Null(taskList.GetTask(2).CreatedDate);
        }
        
        [Fact]
        public void AddEnsureCreatedDate()
        {
            var hasCreatedDate = "2025-04-03 Has a created date";
            var noCreatedDate = "Has no created date";
         
            var taskList = new TaskList();
            
            taskList.Create(hasCreatedDate, ensureCreatedDate: true);
            taskList.Create(noCreatedDate, ensureCreatedDate: true);
            
            Assert.NotNull(taskList.GetTask(1).CreatedDate);
            Assert.NotNull(taskList.GetTask(2).CreatedDate);

            // Make sure that we didn't get an extra date dropped into the task that already had one
            Assert.Equal(hasCreatedDate, taskList.GetTask(1).ToString());
        }

        [Fact]
        public void NumbersInStringOutput()
        { 
            var taskList = new TaskList()
            {
                "this is task 1",
                "this is task 2",
                "this is task 3",
                "this is task 4",
                "this is task 5",
                "this is task 6",
                "this is task 7",
                "this is task 8",
                "this is task 9",
                "this is task 10",
            };

            var output = taskList.ToString();
            Assert.Contains("06", output);

            Console.Write(output);
        }
    }
}