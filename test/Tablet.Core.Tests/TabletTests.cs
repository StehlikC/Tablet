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
        public void Init_GivenNoDirectory_CreatesFileStructureInCurrentDirectory()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet"), "Directory does not exist.");
            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet\objects"), "Directory does not exist.");
            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet\sets"), "Directory does not exist.");
        }

        [Fact]
        public void Init_GivenDirectory_CreatesFileStructureInThatDirectory()
        {
            var mock = new MockFileSystem();
            
            var sut = new Tablet(@"C:\Temp", mock);

            sut.Init();

            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet"), "Directory does not exist.");
            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet\objects"), "Directory does not exist.");
            Assert.True(mock.Directory.Exists(@"C:\Temp\.tablet\sets"), "Directory does not exist.");
        }

        [Fact]
        public void Init_WhenFileStructureAlreadyExists_ThrowsException()
        {
            var mock = new MockFileSystem();

            mock.Directory.CreateDirectory(@"C:\Temp\.tablet");

            var sut = new Tablet(@"C:\Temp", mock);

            Assert.Throws<AlreadyInitializedException>(() => sut.Init());
        }

        [Fact]
        public void HashObject_StoresObjectInObjectsDirectory_ReturnsTheHash()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            var actual = sut.HashObject<int>(1);

            Assert.Equal("356a192b7913b04c54574d18c28d46e6395428ab", actual);
            Assert.True(mock.File.Exists(@"C:\Temp\.tablet\objects\35\6a192b7913b04c54574d18c28d46e6395428ab"), "File does not exist.");
        }

        [Fact]
        public void HashObject_StoresCompressedDataInObjectsDirectory_ReturnsTheHash()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            var actual = sut.HashObject<int>(1);

            Assert.Equal("356a192b7913b04c54574d18c28d46e6395428ab", actual);
            Assert.Equal("9996100969624815432264886412158224202226146212926120718818993514401231101248998781054237136199130", String.Join("", mock.File.ReadAllBytes(@"C:\Temp\.tablet\objects\35\6a192b7913b04c54574d18c28d46e6395428ab")));
        }

        [Fact]
        public void HashObjectWithKey_StoresCompressedDataInKeyedObjectDirectory_ReturnsTheHash()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            var actual = sut.HashObjectWithKey<Fake, string>(new Fake { Value = 1 }, k => Convert.ToString(k.Value));

            Assert.Equal("356a192b7913b04c54574d18c28d46e6395428ab", actual);
            Assert.True(mock.File.Exists(@"C:\Temp\.tablet\sets\35\6a192b7913b04c54574d18c28d46e6395428ab"), "File does not exist.");
        }

        [Fact]
        public void GetObjectSet_ReturnsEnumerableObjects()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            sut.HashObjectWithKey<Fake, string>(new Fake { Value = 1 }, k => Convert.ToString(k.Value));

            var actual = sut.GetObjectSet<Fake, int>(1);

            Assert.Equal(new List<Fake> { new Fake { Value = 1 } }, actual, new FakeComparer());
        }

        [Fact]
        public void GetObjectSet_WhenCalledTwice_ReturnsEnumerableBothObjects()
        {
            var mock = new MockFileSystem();

            mock.Directory.SetCurrentDirectory(@"C:\Temp");

            var sut = new Tablet(mock);

            sut.Init();

            sut.HashObjectWithKey<Fake, string>(new Fake { Value = 1 }, k => Convert.ToString(k.Value));
            sut.HashObjectWithKey<Fake, string>(new Fake { Value = 1 }, k => Convert.ToString(k.Value));

            var actual = sut.GetObjectSet<Fake, int>(1);

            Assert.Equal(new List<Fake> { new Fake { Value = 1 }, new Fake { Value = 1 } }, actual, new FakeComparer());
        }

        [Fact(Skip="NCrunch Problems")]
        public void HashObjectLoadTest()
        {
            if (Directory.Exists(@"C:\Temp"))
                Directory.Delete(@"C:\Temp", true);

            var sut = new Tablet(@"C:\Temp");

            sut.Init();

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            for (int i = 0; i < 1000; i++)
            {
                sut.HashObjectWithKey<Fake, string>(new Fake { Value = 1 }, k => Convert.ToString(k.Value));
            }

            watch.Stop();

            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1));

            //if (Directory.Exists(@"C:\Temp"))
                //Directory.Delete(@"C:\Temp", true);
        }
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
