# Tablet

![Tablet](http://media.giphy.com/media/JATUTMsZw2s5a/giphy.gif)

Tablet is a basic key-value store. Instead of storing everything in memory, it stores everything on disk.

#### Repository

When initialized, the following folder structure is created:

```
.tablet\
  objects\
```

#### Objects

Objects are added to a ```IList<T>```, serialized, then compressed and written to disk. When new objects are added to a key, the objects are uncompressed, deserialized and casted into a ```IList<T>```. The new object is then added to the ```IList<T>``` and re-serialized...compressed...and stored.

Objects follow a ```git``` inspired storage method. Basically the key of the object you're going to ```Push``` is ```SHA1``` hashed and a directory is created with the first two characters of the ```SHA1``` and a file with the last 38 characters of the ```SHA1```

i.e.

```
.tablet\
  objects\
    35\
      6a192b7913b04c54574d18c28d46e6395428ab
```
**Needs Improvement:** Instead of just hashing the key, it should probably hash ```"typeof(T) key"``` otherwise objects of different types will end in the same bucket.


#### Performance

Inserting 1000 objects into the same bucket in Tablet one by one takes about 8 seconds.

## 

**tl;dr**  This was a **bad** idea.
