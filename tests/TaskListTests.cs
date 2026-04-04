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
        public void SelectMany()
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
            tl.SaveTasks(tempTaskFile);

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
                tl.SaveTasks(tempTasksFile);

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
            _ = tl.AsEnumerable();
        }

        [Fact]
        public void Load_From_Stream_Repeated()
        {
            var s = new Stopwatch();

            s.Start();
            for (int n = 0; n < 500; n++)
            {
                using FileStream fs = File.OpenRead(_testDataPath);
                var tl = new TaskList();

                tl.LoadTasks(fs);
            }
            s.Stop();

            Debug.WriteLine(s.Elapsed);
        }

        [Fact]
        public void Load_From_Stream()
        {
            using FileStream fs = File.OpenRead(_testDataPath);
            var tl = new TaskList();

            tl.LoadTasks(fs);

            Assert.Equal(8, tl.Count);
        }

        [Fact]
        public void Save_To_Stream()
        {
            string tempTaskFile = CreateTempTasksFile();

            var taskList = new TaskList();

            using (FileStream fs = File.OpenRead(tempTaskFile))
            {
                taskList.LoadTasks(fs);
            }

            taskList.Add("This task should end up in both lists");

            string tempTaskFileCopy = CreateTempTasksFile();

            using (FileStream fs = File.OpenWrite(tempTaskFileCopy))
            {
                taskList.SaveTasks(fs);
            }

            var tl2 = new TaskList(tempTaskFileCopy);

            Assert.Equal(taskList.Count, tl2.Count);
        }

        [Fact]
        public void Search()
        {
            string tempTasksFile = CreateTempTasksFile();
            var tl = new TaskList(tempTasksFile);

            // There should be two tasks which contain the term 'foo'
            var fooTaskList = tl.Search("foo");
            Assert.NotNull(fooTaskList);
            Assert.Equal(2, fooTaskList.ToNumberedOutput().Count());

            // Search should be case insenstive
            var caseInsensitiveTaskList = tl.Search("Foo");
            Assert.NotNull(caseInsensitiveTaskList);
            Assert.Equal(2, caseInsensitiveTaskList.ToNumberedOutput().Count());

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
        public void LoadTasksFromString()
        {
            var text = @"
this is the first task
this is the second task

the previous line was blank";

            var tl = new TaskList();
            tl.LoadTasksFromString(text);

            Assert.Equal(5, tl.Count);
            Assert.True(tl.Search("previous").Any());
        }

        [Fact]
        public void UpdateTaskPriority()
        {
            var text = @"this is the first task
this is the second task
";

            var tl = new TaskList();
            tl.LoadTasksFromString(text);

            Assert.Null(tl.GetTask(1).Priority);

            tl.SetItemPriority(1, 'a');
            Assert.Equal('A', tl.GetTask(1).Priority);
        }
    }
}