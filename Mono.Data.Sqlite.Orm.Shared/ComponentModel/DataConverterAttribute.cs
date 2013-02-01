using System.Reflection;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    using System;
    using System.Linq;
    using DataConverter;
    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DataConverterAttribute : Attribute
    {
        public DataConverterAttribute(Type dataConverter, Type storageType)
        {
            if (dataConverter == null)
            {
                throw new ArgumentNullException("dataConverter", "A converter must be specified.");
            }

            if (storageType == null)
            {
                throw new ArgumentNullException("storageType", "A storage type must be specified.");
            }

            if (!dataConverter.GetInterfaces().Contains(typeof(IDataConverter)))
            {
                throw new ArgumentException("The converter must inherit from IDataConverter.", "dataConverter");
            }

            var constructors = dataConverter.GetConstructors();
            if (constructors.All(x => !x.IsPublic || x.GetParameters().Length != 0))
            {
                throw new ArgumentException("The converter must have a public parameterless constructor.",
                                            "dataConverter");
            }

            StorageType = storageType;
            DataConverter = dataConverter;
            Parameter = null;
        }

        public Type StorageType { get; private set; }
        public Type DataConverter { get; private set; }
        public object Parameter { get; set; }
    }
}