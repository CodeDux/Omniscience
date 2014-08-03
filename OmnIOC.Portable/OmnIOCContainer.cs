﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace OmnIOC.Portable
{
    /// <summary>
    ///     Simple object factory / DiContainer
    ///     Implement custom methods in a partial file since any changes will be lost on update.
    /// </summary>
    public class OmnIOCContainer
    {
        private static readonly Lazy<OmnIOCContainer> DefaultContainer =
            new Lazy<OmnIOCContainer>(() => new OmnIOCContainer(), LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly Dictionary<string, object> _typesCollection = new Dictionary<string, object>();

        private OmnIOCContainer()
        {
            ThrowOnMissingType = true;
        }

        public static OmnIOCContainer Default
        {
            get { return DefaultContainer.Value; }
        }

        /// <summary>
        ///     Should DiDay return default or throw when registration is missing for a type.
        /// </summary>
        public bool ThrowOnMissingType { get; set; }

        /// <summary>
        ///     Exposed to test project
        /// </summary>
        internal void Clear()
        {
            _typesCollection.Clear();
        }

        /// <summary>
        ///     Register factory for <see cref="T" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>
        public void Register<T>(Func<OmnIOCContainer, T> factory, string name = null)
        {
            SetType<T>(name, new FactoryContainer<T>(factory));
        }

        /// <summary>
        ///     Register instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public void RegisterInstance<T>(T instance, string name = null)
        {
            SetType<T>(name, new InstanceContainer<T>(instance));
        }

        private void SetType<T>(string name, object factoryContainer)
        {
            if(_lock.IsReadLockHeld)
                throw new LockRecursionException("Can't register while resolving.");

            var key = FormatKey<T>(name);
            try
            {
                _lock.EnterWriteLock();
                _typesCollection[key] = factoryContainer;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Resolve implementation of <see cref="T" />
        /// </summary>
        /// <typeparam name="T">Type to be resolved</typeparam>
        /// <param name="name"></param>
        /// <param name="throwOnMissingType">Overrides ThrowOnMissingType property</param>
        /// <returns></returns>
        public T Resolve<T>(string name = null, bool? throwOnMissingType = null)
        {
            if (!throwOnMissingType.HasValue)
                throwOnMissingType = ThrowOnMissingType;

            var type = typeof (T);
            try
            {
                _lock.EnterReadLock();
                object container;
                var key = name == null ? type.FullName : FormatKey<T>(name);

                if (_typesCollection.TryGetValue(key, out container))
                {
                    var factory = (FuncContainer<T>) container;
                    return factory.Get(this);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (throwOnMissingType.Value)
                throw new Exception(string.Format("There is no type registered for {0}", type.FullName));

            return default(T);
        }

        private static string FormatKey<T>(string name)
        {
            var typeName = typeof (T).FullName;

            return name == null ? typeName : string.Format("{0}_{1}", typeName, name);
        }
    }
}