using System;
using System.Threading;

namespace Mono.Data.Sqlite.Orm
{
    public static class AsyncExtensions
    {
        public static IDisposable Lock(this object toLock)
        {
            return new LockWrapper(toLock);
        }

        private class LockWrapper : IDisposable
        {
            private readonly object _lockPoint;

            public LockWrapper(object lockPoint)
            {
                this._lockPoint = lockPoint;
                Monitor.Enter(this._lockPoint);
            }

            public void Dispose()
            {
                Monitor.Exit(this._lockPoint);
            }
        }
    }
}
