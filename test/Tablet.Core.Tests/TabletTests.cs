using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Tablet.Core.Metadata;
using Xunit;

namespace Tablet.Core.Tests
{
    public class TabletTests
    {
        [Fact]
        public void Init_given_no_directory_path_it_creates_directory_structure_in_the_current_directory_path()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet"), "Directory does not exist.");
            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet\objects"), "Directory does not exist.");
        }

        [Fact]
        public void Init_given_directory_path_it_creates_directory_structure_in_that_directory_path()
        {
            var mock = new MockFileSystem();
            
            var sut = new Tablet(@"C:\Temp", mock);

            sut.Init();

            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet"), "Directory does not exist.");
            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet\objects"), "Directory does not exist.");
        }

        [Fact]
        public void Init_when_file_structure_exists_throws_exception()
        {
            var mock = new MockFileSystem();

            mock.Directory.CreateDirectory(@"C:\Temp\.tablet");

            var sut = new Tablet(@"C:\Temp", mock);

            Assert.Throws<AlreadyInitializedException>(() => sut.Init());
        }

        [Fact]
        public void Push_when_no_bucket_exists_yet_creates_bucket_then_returns_hash()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            var p1 = sut.Push(new Fake { Value = 1 }, k => k.Value);

            Assert.Equal("356a192b7913b04c54574d18c28d46e6395428ab", p1);
            Assert.True(mock.File.Exists(@"C:\Temp\.tablet\objects\35\6a192b7913b04c54574d18c28d46e6395428ab"), "File does not exist.");
        }

        [Fact]
        public void Push_when_bucket_exists_it_reads_the_bucket_and_adds_object_then_returns_hash()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            var p1 = sut.Push(new Fake { Value = 1 }, k => k.Value);
            var p2 = sut.Push(new Fake { Value = 1 }, k => k.Value);

            Assert.Equal("356a192b7913b04c54574d18c28d46e6395428ab", p1);
            Assert.True(mock.File.Exists(@"C:\Temp\.tablet\objects\35\6a192b7913b04c54574d18c28d46e6395428ab"), "File does not exist.");

            Assert.Equal("356a192b7913b04c54574d18c28d46e6395428ab", p2);
            Assert.True(mock.File.Exists(@"C:\Temp\.tablet\objects\35\6a192b7913b04c54574d18c28d46e6395428ab"), "File does not exist.");
        }

        [Fact]
        public void Get_when_bucket_exists_returns_list_of_objects_with_key()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            sut.Push(new Fake { Value = 1 }, k => k.Value);

            var actual = sut.Get<Fake, int>(1);

            Assert.Equal(new List<Fake> { new Fake { Value = 1 } }, actual, new FakeComparer());
        }

        [Fact]
        public void Get_when_bucket_exists_with_many_objects_returns_list_of_objects_with_key()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            sut.Push(new Fake { Value = 1 }, k => k.Value);
            sut.Push(new Fake { Value = 1 }, k => k.Value);

            var actual = sut.Get<Fake, int>(1);

            Assert.Equal(new List<Fake> { new Fake { Value = 1 }, new Fake { Value = 1 } }, actual, new FakeComparer());
        }

        [Serializable]
        public class Fake
        {
            public int Value { get; set; }
        }

        public class FakeComparer : IEqualityComparer<Fake>
        {
            public bool Equals(Fake x, Fake y)
            {
                return x.Value == y.Value;
            }

            public int GetHashCode(Fake obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
