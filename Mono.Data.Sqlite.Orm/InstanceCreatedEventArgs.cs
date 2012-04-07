using System;

namespace Mono.Data.Sqlite.Orm
{
    public class InstanceCreatedEventArgs : EventArgs
    {
        public InstanceCreatedEventArgs(object instance)
        {
            Instance = instance;
        }

        public object Instance { get; private set; }
    }
}
