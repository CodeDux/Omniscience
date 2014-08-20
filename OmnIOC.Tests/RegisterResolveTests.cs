﻿using System;
using System.Linq;
using System.Threading;
using OmnIOC.Portable;
using Xunit;

namespace OmnIOC.Tests
{
    public class RegisterResolveTests
    {
        [Fact]
        public void Factory()
        {
            OmnIOCContainer.Default.Clear();
            OmnIOCContainer.Default.Register(o => new TestClass1());
            OmnIOCContainer.Default.Register(o => new TestClass2(o.Resolve<TestClass1>()));
            var testClass = OmnIOCContainer.Default.Resolve<TestClass2>();
            Assert.NotNull(testClass);
            Assert.NotNull(testClass.Inner);
        }

        [Fact]
        public void NamedFactory()
        {
            OmnIOCContainer.Default.Clear();
            OmnIOCContainer.Default.Register(o => new TestClass1(), "1a");
            OmnIOCContainer.Default.Register(o => new TestClass2(o.Resolve<TestClass1>("1A")), "2X");
            var testClass = OmnIOCContainer.Default.Resolve<TestClass2>("2x");
            Assert.NotNull(testClass);
            Assert.NotNull(testClass.Inner);
        }

        [Fact]
        public void GetAllNamesForType()
        {
            OmnIOCContainer.Default.Clear();
            OmnIOCContainer.Default.Register(o => new TestClass1(), "1a");
            OmnIOCContainer.Default.RegisterInstance(new TestClass1(), "2a");
            var names = OmnIOCContainer.Default.ResolveAllNames<TestClass1>();
            Assert.NotNull(names);
            var namesList = names.ToList();
            Assert.True(namesList.Count == 2);
            Assert.Contains("1a",namesList);
            Assert.Contains("2a", namesList);
        }

        [Fact]
        public void Instance()
        {
            OmnIOCContainer.Default.Clear();
            OmnIOCContainer.Default.RegisterInstance(new TestClass1());
            OmnIOCContainer.Default.RegisterInstance(new TestClass2(OmnIOCContainer.Default.Resolve<TestClass1>()));
            var testClass = OmnIOCContainer.Default.Resolve<TestClass2>();
            Assert.NotNull(testClass);
            Assert.NotNull(testClass.Inner);
        }

        [Fact]
        public void NamedInstance()
        {
            OmnIOCContainer.Default.Clear();
            OmnIOCContainer.Default.RegisterInstance(new TestClass1(), "1a");
            OmnIOCContainer.Default.RegisterInstance(new TestClass2(OmnIOCContainer.Default.Resolve<TestClass1>("1A")), "2X");
            var testClass = OmnIOCContainer.Default.Resolve<TestClass2>("2x");
            Assert.NotNull(testClass);
            Assert.NotNull(testClass.Inner);
        }

        [Fact]
        public void RegisterInResolveShouldFail()
        {
            OmnIOCContainer.Default.Clear();
            OmnIOCContainer.Default.Register(o =>
            {
                o.Register(o2 => new TestClass1());
                return new TestClass2(o.Resolve<TestClass1>());
            });

            Assert.Throws<LockRecursionException>(() => OmnIOCContainer.Default.Resolve<TestClass2>());
        }

        public class TestClass1
        {
        }

        public class TestClass2
        {
// ReSharper disable NotAccessedField.Local
            public readonly TestClass1 Inner;
// ReSharper restore NotAccessedField.Local

            public TestClass2(TestClass1 inner)
            {
                Inner = inner;
            }
        }
    }
}