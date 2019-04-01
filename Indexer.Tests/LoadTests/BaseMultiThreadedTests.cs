﻿using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Indexer.Indexes;
using Indexer.Tests.Base;
using NUnit.Framework;

namespace Indexer.Tests.LoadTests
{
    [TestFixture]
    public abstract class BaseMultiThreadedTests : TestsBase
    {
        [Test]
        public void Add_And_Read_Simultaneously()
        {
            var exceptionsCount = 0;
            var index = this.GetNewIndex();
            Action addAction = () =>
            {
                try
                {
                    for (var i = 0; i < 10; i++)
                    {
                        var lines = TestDataGenerator.GetRandomLines(Environment.TickCount + i, 1000);
                        for (var j = 0; j < lines.Length; j++)
                        {
                            index.Add(lines[j], j + 1, "some doc");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Interlocked.Increment(ref exceptionsCount);
                    throw;
                }
            };
            var addTasks = new Task[2];
            for (var i = 0; i < addTasks.Length; i++)
            {
                addTasks[i] = new Task(addAction);
                addTasks[i].Start();
            }

            Action readAction = () =>
            {
                try
                {
                    for (var i = 0; i < 1000; i++)
                    {
                        var phrase = TestDataGenerator.GetSearchPhrase(Environment.TickCount + i);
                        index.Find(phrase);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Interlocked.Increment(ref exceptionsCount);
                    throw;
                }
            };
            var readTasks = new Task[2];
            for (var i = 0; i < readTasks.Length; i++)
            {
                readTasks[i] = new Task(readAction);
                readTasks[i].Start();
            }

            Task.WaitAll(addTasks);
            Task.WaitAll(readTasks);

            exceptionsCount.Should().Be(0);
        }

        protected abstract IInvertedIndex GetNewIndex();
    }
}