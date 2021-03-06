using System.Collections.Generic;
using System.Threading;

namespace MoMMI.Core.Logging
{
    internal sealed partial class LogManager : ILogManager
    {
        public const string Root = "root";

        private readonly Sawmill rootSawmill;
        public ISawmill RootSawmill => rootSawmill;

        private readonly Dictionary<string, Sawmill> sawmills = new Dictionary<string, Sawmill>();
        private readonly ReaderWriterLockSlim _sawmillsLock = new ReaderWriterLockSlim();

        public ISawmill GetSawmill(string name)
        {
            _sawmillsLock.EnterReadLock();
            try
            {
                if (sawmills.TryGetValue(name, out var sawmill))
                {
                    return sawmill;
                }
            }
            finally
            {
                _sawmillsLock.ExitReadLock();
            }

            _sawmillsLock.EnterWriteLock();
            try
            {
                return _getSawmillUnlocked(name);
            }
            finally
            {
                _sawmillsLock.ExitWriteLock();
            }
        }

        private Sawmill _getSawmillUnlocked(string name)
        {
            if (sawmills.TryGetValue(name, out var sawmill))
            {
                return sawmill;
            }

            var index = name.LastIndexOf('.');
            string parentName;
            if (index == -1)
            {
                parentName = Root;
            }
            else
            {
                parentName = name.Substring(0, index);
            }

            var parent = _getSawmillUnlocked(parentName);
            sawmill = new Sawmill(parent, name);
            sawmills.Add(name, sawmill);
            return sawmill;
        }

        public LogManager()
        {
            rootSawmill = new Sawmill(null, Root)
            {
                Level = LogLevel.Debug,
            };
            sawmills[Root] = rootSawmill;
        }
    }

}
