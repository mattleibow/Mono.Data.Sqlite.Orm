﻿using Mono.Data.Sqlite.Orm.Tests.WindowsPhone8.Resources;

namespace Mono.Data.Sqlite.Orm.Tests.WindowsPhone8
{
    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources { get { return _localizedResources; } }
    }
}