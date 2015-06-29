using System;
using JetBrains.Annotations;

namespace Tablet.Core.Metadata
{
    public class AlreadyInitializedException : Exception
    {
        public AlreadyInitializedException()
        {
        }

        public AlreadyInitializedException([NotNull] string message) : base(message)
        {
        }

        public AlreadyInitializedException([NotNull] string message, [CanBeNull] Exception innerException) : base(message, innerException)
        {
        }
    }
}
