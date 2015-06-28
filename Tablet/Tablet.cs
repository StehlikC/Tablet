using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;

namespace Tablet
{
    public class Tablet
    {
        private const string Directory = ".tablet";
        private const string ObjectsDirectory = "objects";
        private const string SetsDirectory = "sets";

        private readonly IFileSystem _fileSystem;
        private string _root;

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
                _fileSystem.Directory.CreateDirectory(String.Join(@"\", _root, Directory, SetsDirectory));
            }
            else
            {
                throw new TabletAlreadyInitializedException();
            }
        }

        public string HashObject<T>(T obj)
        {
            var hash = BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(obj.ToString()))).Replace("-", "").ToLower();

            if (!_fileSystem.Directory.Exists(String.Join(@"\", _root, Directory, ObjectsDirectory, hash.ToCharArray().Take(2))))
            {
                _fileSystem.Directory.CreateDirectory(String.Join(@"\", _root, Directory, ObjectsDirectory, String.Join("", hash.ToCharArray().Take(2))));

                using (var file = _fileSystem.File.Create(String.Join(@"\", _root, Directory, ObjectsDirectory, String.Join("", hash.ToCharArray().Take(2)), String.Join("", hash.ToCharArray().Skip(2).Take(38)))))
                {
                    using (var stream = new MemoryStream())
                    {
                        var bf = new BinaryFormatter();

                        using (var compression = new DeflateStream(stream, CompressionMode.Compress, true))
                        {
                            bf.Serialize(compression, obj);
                        }

                        stream.WriteTo(file);
                    }
                }
            }

            return hash;
        }

        public string HashObjectWithKey<T, TProperty>(T obj, Func<T, TProperty> selector)
        {
            var hash = BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(Convert.ToString(selector.Invoke(obj))))).Replace("-", "").ToLower();

            using (var os = new MemoryStream())
            {
                var bf = new BinaryFormatter();

                using (var ds = new DeflateStream(os, CompressionMode.Compress, true))
                {
                    var set = GetObjectSet<T, TProperty>(selector.Invoke(obj));

                    set.Add(obj);

                    bf.Serialize(ds, set);
                }

                if (!_fileSystem.Directory.Exists(String.Join(@"\", _root, Directory, SetsDirectory, hash.ToCharArray().Take(2))))
                {
                    _fileSystem.Directory.CreateDirectory(String.Join(@"\", _root, Directory, SetsDirectory, String.Join("", hash.ToCharArray().Take(2))));

                    using (var fs = _fileSystem.File.Open(String.Join(@"\", _root, Directory, SetsDirectory, String.Join("", hash.ToCharArray().Take(2)), String.Join("", hash.ToCharArray().Skip(2).Take(38))), FileMode.Create))
                    {
                        os.WriteTo(fs);
                    }
                }
            }

            return hash;
        }

        public IList<T> GetObjectSet<T, TProperty>(TProperty key)
        {
            var hash = BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(Convert.ToString(key)))).Replace("-", "").ToLower();

            try
            {
                using (var fs = _fileSystem.File.Open(String.Join(@"\", _root, Directory, SetsDirectory, String.Join("", hash.ToCharArray().Take(2)), String.Join("", hash.ToCharArray().Skip(2).Take(38))), FileMode.Open, FileAccess.Read))
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
