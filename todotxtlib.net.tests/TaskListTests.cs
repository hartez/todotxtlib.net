using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace todotxtlib.net.tests
{
    [TestFixture]
    class TaskListTests
    {
        private string _testDataPath = "testtasks.txt";

        [Test]
        public void Construct()
        {
            var tl = new TaskList(_testDataPath);
        }


        [Test]
        public void Load_From_File()
        {
            var tl = new TaskList(_testDataPath);
            var tasks = tl.AsEnumerable();
        }

        [Test]
        public void Add_ToCollection()
        {
            var task = new Task("(B) Add_ToCollection +test @task");

            var tl = new TaskList(_testDataPath);

            var tasks = tl.ToList();
            tasks.Add(task);

            tl.Add(task);

            var newTasks = tl.ToList();

            Assert.AreEqual(tasks.Count, newTasks.Count);

            for (int i = 0; i < tasks.Count; i++)
                Assert.AreEqual(tasks[i].ToString(), newTasks[i].ToString());
        }

        private string CreateTempTasksFile()
        {
            string tempTaskFile = Path.GetRandomFileName();
            File.Copy(_testDataPath, tempTaskFile, true);
            return tempTaskFile;
        }

        [Test]
        public void Add_ToFile()
        {
            // Create a copy of test data so we can leave the original alone
            string tempTaskFile = CreateTempTasksFile();

            var fileContents = File.ReadAllLines(tempTaskFile).ToList();
            fileContents.Add("(B) Add_ToFile +test @task");

            var task = new Task(fileContents.Last());
            var tl = new TaskList(tempTaskFile);
            tl.Add(task);
            tl.SaveTasks(tempTaskFile);

            var newFileContents = File.ReadAllLines(tempTaskFile);
            CollectionAssert.AreEquivalent(fileContents, newFileContents);

            // Clean up
            File.Delete(tempTaskFile);
        }

        [Test]
        public void Add_To_Empty_File()
        {
            // v0.3 and earlier contained a bug where a blank task was added
            string tempTaskFile = CreateTempTasksFile();
            File.WriteAllLines(tempTaskFile, new string[] { }); // empties the file

            var tl = new TaskList(tempTaskFile);
            tl.Add(new Task("A task"));

            Assert.AreEqual(1, tl.Count());

            // Clean up
            File.Delete(tempTaskFile);
        }

        [Test]
        public void Add_Multiple()
        {
            var tl = new TaskList(_testDataPath);
            var c = tl.Count();

            var task = new Task("Add_Multiple task one");
            tl.Add(task);

            var task2 = new Task("Add_Multiple task two");
            tl.Add(task2);

            Assert.AreEqual(c + 2, tl.Count());
        }

        [Test]
        public void Delete_InCollection()
        {
            var task = new Task("(B) Delete_InCollection +test @task");
            var tl = new TaskList(_testDataPath);
            tl.Add(task);

            var tasks = tl.ToList();
            tasks.Remove(tasks.Last());

            tl.Delete(task);

            var newTasks = tl.ToList();

            Assert.AreEqual(tasks.Count, newTasks.Count);

            for (int i = 0; i < tasks.Count; i++)
                Assert.AreEqual(tasks[i].ToString(), newTasks[i].ToString());
        }

        [Test]
        public void Delete_InFile()
        {
            string tempTasksFile = CreateTempTasksFile();
            try
            {
                string[] fileLines = File.ReadAllLines(tempTasksFile);
                List<string> fileContents = fileLines.ToList();
                var task = new Task(fileContents.Last());
                fileContents.Remove(fileContents.Last());

                var tl = new TaskList(tempTasksFile);
                tl.Delete(task);
                tl.SaveTasks(tempTasksFile);

                string[] newFileContents = File.ReadAllLines(tempTasksFile);
                CollectionAssert.AreEquivalent(fileContents, newFileContents);
            }
            finally
            {
                File.Delete(tempTasksFile);
            }
        }

        [Test]
        public void ToggleComplete_On_InCollection()
        {
            var task = new Task("(B ToggleComplete_On_InCollection +test @task");
            var tl = new TaskList(_testDataPath);
            tl.Add(task);

            task = tl.Last();

            task.ToggleCompleted();

            task = tl.Last();

            Assert.IsTrue(task.Completed);
        }


        [Test]
        public void ToggleComplete_Off_InCollection()
        {
            // Not complete - doesn't include completed date
            var task = new Task("X (B) ToggleComplete_Off_InCollection +test @task");
            var tl = new TaskList(_testDataPath);
            tl.Add(task);

            task = tl.Last();

            task.ToggleCompleted();

            task = tl.Last();

            Assert.IsTrue(task.Completed);

            var task2 = new Task("X 2011-02-25 ToggleComplete_Off_InCollection +test @task");
            
            tl.Add(task2);

            task = tl.Last();

            task.ToggleCompleted();

            task = tl.Last();

            Assert.IsFalse(task.Completed);
        }

        [Test]
        public void Update_InCollection()
        {
            var task = new Task("(B) Update_InCollection +test @task");

            var tl = new TaskList(_testDataPath);
            tl.Add(task);

            var task2 = new Task(task.Raw);
            task2.ToggleCompleted();

            tl.Update(task, task2);

            var newTask = tl.Last();
            Assert.IsTrue(newTask.Completed);
        }
    }
}