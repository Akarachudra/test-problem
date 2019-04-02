﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FluentAssertions;
using Indexer.Indexes;
using Indexer.Tests.Base;
using Indexer.Tokens;
using Indexer.Watch;
using Moq;
using NUnit.Framework;

namespace Indexer.Tests
{
    [TestFixture]
    public class IndexServiceTests : TestsBase
    {
        private readonly Mock<IDirectoryObserver> observerMock = new Mock<IDirectoryObserver>();

        [TestCase(0)]
        [TestCase(-1)]
        public void Throw_Exception_If_Build_Tasks_Count_Is_Zero_Or_Lower(int tasksCount)
        {
            Action createAction = () => new IndexService(this.PathToFiles, new InvertedIndex(new DefaultTokenizer()), this.observerMock.Object, tasksCount);

            createAction.Should().Throw<ArgumentException>().And.Message.Should().Be("Invalid index build tasks count");
        }

        [Test]
        public void Throw_Exception_If_Passed_Path_Does_Not_Exists()
        {
            Action createAction = () => new IndexService("not exists path", new InvertedIndex(new DefaultTokenizer()), this.observerMock.Object);

            createAction.Should().Throw<ArgumentException>().And.Message.Should().Be("Path doesn't exists");
        }

        [Test]
        public void Throw_Exception_If_Passed_Index_Is_Null()
        {
            Action createAction = () => new IndexService(this.PathToFiles, null, this.observerMock.Object);

            createAction.Should().Throw<ArgumentException>().And.Message.Should().Be("Index should not be null");
        }

        [Test]
        public void Throw_Exception_If_Passed_DirectoryObserver_Is_Null()
        {
            Action createAction = () => new IndexService(this.PathToFiles, new InvertedIndex(new DefaultTokenizer()), null);

            createAction.Should().Throw<ArgumentException>().And.Message.Should().Be("Directory observer should not be null");
        }

        [Test]
        public void Find_Returns_Result_From_Index()
        {
            var indexMock = new Mock<IInvertedIndex>();
            var expectedResult = new List<DocumentPosition> { new DocumentPosition { RowNumber = 5 } };
            indexMock.Setup(x => x.Find("query phrase")).Returns(expectedResult);
            var indexService = new IndexService(this.PathToFiles, indexMock.Object, this.observerMock.Object);

            var result = indexService.Find("query phrase");

            result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Start_Directory_Observer_At_Ctor()
        {
            var indexMock = new Mock<IInvertedIndex>();
            var localObserverMock = new Mock<IDirectoryObserver>();

            new IndexService(this.PathToFiles, indexMock.Object, localObserverMock.Object);

            localObserverMock.Verify(x => x.Start(), Times.Once);
        }

        [Test]
        public void Could_Not_Start_Two_Times()
        {
            var indexMock = new Mock<IInvertedIndex>();
            var indexService = new IndexService(this.PathToFiles, indexMock.Object, this.observerMock.Object);

            Action startAction = () => indexService.StartBuildIndex();

            startAction.Should().NotThrow();
            startAction.Should().Throw<ArgumentException>().And.Message.Should().Be("Already started");
        }

        [Test]
        public void Can_Restart()
        {
            var indexMock = new Mock<IInvertedIndex>();
            var indexService = new IndexService(this.PathToFiles, indexMock.Object, this.observerMock.Object);

            indexService.StartBuildIndex();
            indexService.StopBuildIndex();
            indexService.StartBuildIndex();
        }

        [Test]
        public void File_Is_Indexed_On_Created_Event()
        {
            var indexMock = new Mock<IInvertedIndex>();
            var indexService = new IndexService(this.PathToFiles, indexMock.Object, this.observerMock.Object);
            File.WriteAllLines(this.FirstFilePath, new[] { "first line", "second line" });

            indexService.StartBuildIndex();
            this.observerMock.Raise(mock => mock.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, this.PathToFiles, FirstFileName));

            Thread.Sleep(1000);
            indexMock.Verify(x => x.Add("first line", 1, this.FirstFilePath), Times.Once);
            indexMock.Verify(x => x.Add("second line", 2, this.FirstFilePath), Times.Once);
        }

        [Test]
        public void Can_Stop_Index_Building()
        {
            var indexMock = new Mock<IInvertedIndex>();
            var indexService = new IndexService(this.PathToFiles, indexMock.Object, this.observerMock.Object);
            File.WriteAllLines(this.FirstFilePath, new[] { "first line", "second line" });

            indexService.StartBuildIndex();
            indexService.StopBuildIndex();
            Thread.Sleep(1000);
            this.observerMock.Raise(mock => mock.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, this.PathToFiles, FirstFileName));

            Thread.Sleep(1000);
            indexMock.VerifyNoOtherCalls();
        }
    }
}