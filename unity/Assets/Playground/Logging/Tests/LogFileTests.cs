using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace BaseGame.Playground.Logging.Tests
{
    public class LogFileTests
    {
        private static readonly DateTime Start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private string _directory;

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "LogFileTests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void NewLogFile_FirstLine_IsVersion()
        {
            string[] lines = WriteAndRead(file => { });

            Assert.That(lines[0], Is.EqualTo("Version 1.2.3"));
        }

        [Test]
        public void Write_Error_KeepsMessageAndStackTrace()
        {
            string[] lines = WriteAndRead(file => file.Write("Stat 'A': broken", "at Foo()", LogType.Error));

            Assert.That(lines[1], Does.EndWith("[Error] Stat 'A': broken"));
            Assert.That(lines[2], Is.EqualTo("at Foo()"));
        }

        [Test]
        public void Write_PlainLog_LeavesOutStackTrace()
        {
            string[] lines = WriteAndRead(file => file.Write("hello", "at Foo()", LogType.Log));

            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[1], Does.EndWith("[Log] hello"));
        }

        [Test]
        public void Write_AfterDispose_DoesNothing()
        {
            var file = new LogFile(_directory, "1.2.3", 5, Start);
            file.Dispose();

            Assert.DoesNotThrow(() => file.Write("late", "", LogType.Log));
            Assert.That(File.ReadAllLines(file.FilePath).Length, Is.EqualTo(1));
        }

        [Test]
        public void Write_FromManyThreads_KeepsEveryLine()
        {
            string[] lines = WriteAndRead(file => Parallel.For(0, 200, i => file.Write($"line {i}", "", LogType.Log)));

            Assert.That(lines.Count(l => l.Contains("[Log] line ")), Is.EqualTo(200));
        }

        [Test]
        public void NewLogFile_BeyondMaxFiles_DeletesOldest()
        {
            string[] paths = new string[3];
            for (int i = 0; i < paths.Length; i++)
            {
                using (var file = new LogFile(_directory, "1.2.3", 2, Start.AddSeconds(i)))
                {
                    paths[i] = file.FilePath;
                }
            }

            Assert.That(File.Exists(paths[0]), Is.False);
            Assert.That(File.Exists(paths[1]), Is.True);
            Assert.That(File.Exists(paths[2]), Is.True);
        }

        [Test]
        public void NewLogFile_BadArguments_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LogFile(" ", "1.2.3", 5, Start));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogFile(_directory, "1.2.3", 0, Start));
        }

        private string[] WriteAndRead(Action<LogFile> write)
        {
            string path;
            using (var file = new LogFile(_directory, "1.2.3", 5, Start))
            {
                path = file.FilePath;
                write(file);
            }

            return File.ReadAllLines(path);
        }
    }
}
