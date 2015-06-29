using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Tablet.Core.Metadata;

namespace Tablet.Core
{
    public class Tablet
    {
        private const string Directory = ".tablet";
        private const string ObjectsDirectory = "objects";

        private readonly IFileSystem _fileSystem;
        private readonly string _root;

        public Tablet()
        {
            _fileSystem = new FileSystem();
            _root = _fileSystem.Directory.GetCurrentDirectory();
        }

        public Tablet(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _root = _fileSystem.Directory.GetCurrentDirectory();
        }

        public Tablet(string root)
        {
            _fileSystem = new FileSystem();
            _root = root;
        }

        public Tablet(string root, IFileSystem fileSystem)
        {
            _root = root;
            _fileSystem = fileSystem;
        }

        public void Init()
        {
            if (!_fileSystem.Directory.Exists(String.Join(@"\", _root, Directory)))
            {
                _fileSystem.Directory.CreateDirectory(String.Join(@"\", _root, Directory));
                _fileSystem.Directory.CreateDirectory(String.Join(@"\", _root, Directory, ObjectsDirectory));
            }
            else
            {
                throw new AlreadyInitializedException();
            }
        }

        public string Push<T>(T obj, string key)
        {
            var hash = BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "").ToLower();

            using (var os = new MemoryStream())
            {
                var bf = new BinaryFormatter();

                using (var ds = new DeflateStream(os, CompressionMode.Compress, true))
                {
                    var set = Get<T, string>(key);

                    set.Add(obj);

                    bf.Serialize(ds, set);
                }

                if (!_fileSystem.Directory.Exists(String.Join(@"\", _root, Directory, ObjectsDirectory, hash.ToCharArray().Take(2))))
                {
                    _fileSystem.Directory.CreateDirectory(String.Join(@"\", _root, Directory, ObjectsDirectory, String.Join("", hash.ToCharArray().Take(2))));

                    using (var fs = _fileSystem.File.Open(String.Join(@"\", _root, Directory, ObjectsDirectory, String.Join("", hash.ToCharArray().Take(2)), String.Join("", hash.ToCharArray().Skip(2).Take(38))), FileMode.Create))
                    {
                        os.WriteTo(fs);
                    }
                }
            }

            return hash;
        }

        public string Push<T, TProperty>(T obj, Func<T, TProperty> selector)
        {
            return Push(obj, Convert.ToString(selector.Invoke(obj)));
        }

        public IList<T> Get<T, TProperty>(TProperty key)
        {
            var hash = BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(Convert.ToString(key)))).Replace("-", "").ToLower();

            try
            {
                using (var fs = _fileSystem.File.Open(String.Join(@"\", _root, Directory, ObjectsDirectory, String.Join("", hash.ToCharArray().Take(2)), String.Join("", hash.ToCharArray().Skip(2).Take(38))), FileMode.Open, FileAccess.Read))
                {
                    using (var os = new MemoryStream())
                    {
                        var bf = new BinaryFormatter();

                        using (var ds = new DeflateStream(fs, CompressionMode.Decompress, true))
                        {
                            ds.CopyTo(os);
                        }

                        os.Position = 0;

                        if (os.Length == 0)
                        {
                            return new List<T>();
                        }

                        return (List<T>)bf.Deserialize(os);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                return new List<T>();
            }
            catch(DirectoryNotFoundException)
            {
                return new List<T>();
            }
        }
    }
}
