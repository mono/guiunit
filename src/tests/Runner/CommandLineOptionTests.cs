// *****************************************************
// Copyright 2007, Charlie Poole
//
// Licensed under the Open Software License version 3.0
// *****************************************************

using System;
using System.IO;
using NUnit.Framework;
using Env = NUnit.Env;

namespace NUnitLite.Runner.Tests
{
    [TestFixture]
    class CommandLineOptionTests
    {
        private CommandLineOptions options;

        [SetUp]
        public void CreateOptions()
        {
            options = new CommandLineOptions("-");
        }

        [Test]
        public void TestWaitOption()
        {
            options.Parse( "-wait" );
            Assert.That(options.Error, Is.False);
            Assert.That(options.Wait, Is.True);
        }

        [Test]
        public void TestNoheaderOption()
        {
            options.Parse("-noheader");
            Assert.That(options.Error, Is.False);
            Assert.That(options.NoHeader, Is.True);
        }

        [Test]
        public void OptionNotRecognizedUnlessPrecededByOptionChar()
        {
            options.Parse( "/wait" );
            Assert.That(options.Error, Is.False);
            Assert.That(options.Wait, Is.False);
            Assert.That(options.Parameters, Contains.Item("/wait"));
        }

        [Test]
        public void InvalidOptionProducesError()
        {
            options.Parse( "-junk" );
            Assert.That(options.Error);
            Assert.That(options.ErrorMessage, Is.EqualTo("Invalid option: -junk" + Env.NewLine));
        }

        [Test]
        public void MultipleInvalidOptionsAreListedInErrorMessage()
        {
            options.Parse( "-junk", "-trash", "something", "-garbage" );
            Assert.That(options.Error);
            Assert.That(options.ErrorMessage, Is.EqualTo(
                "Invalid option: -junk" + Env.NewLine +
                "Invalid option: -trash" + Env.NewLine +
                "Invalid option: -garbage" + Env.NewLine));
        }

        [Test]
        public void SingleParameterIsSaved()
        {
            options.Parse("myassembly.dll");
            Assert.That(options.Error, Is.False);
            Assert.That(options.Parameters.Length, Is.EqualTo(1));
            Assert.That(options.Parameters[0], Is.EqualTo("myassembly.dll"));
        }

        [Test]
        public void MultipleParametersAreSaved()
        {
            options.Parse("assembly1.dll", "-wait", "assembly2.dll", "assembly3.dll");
            Assert.That(options.Error, Is.False);
            Assert.That(options.Parameters.Length, Is.EqualTo(3));
            Assert.That(options.Parameters[0], Is.EqualTo("assembly1.dll"));
            Assert.That(options.Parameters[1], Is.EqualTo("assembly2.dll"));
            Assert.That(options.Parameters[2], Is.EqualTo("assembly3.dll"));
        }

        [Test]
        public void TestOptionIsRecognized()
        {
            options.Parse("-test:Some.Class.Name");
            Assert.That(options.Error, Is.False);
            Assert.That(options.Tests.Length, Is.EqualTo(1));
            Assert.That(options.Tests[0], Is.EqualTo("Some.Class.Name"));
        }

        [Test]
        public void MultipleTestOptionsAreRecognized()
        {
            options.Parse("-test:Class1", "-test=Class2", "-test:Class3");
            Assert.That(options.Error, Is.False);
            Assert.That(options.Tests.Length, Is.EqualTo(3));
            Assert.That(options.Tests[0], Is.EqualTo("Class1"));
            Assert.That(options.Tests[1], Is.EqualTo("Class2"));
            Assert.That(options.Tests[2], Is.EqualTo("Class3"));
        }
#if !SILVERLIGHT
        [Test]
        public void TestIncludeOption()
        {
            options.Parse("-include:1,2");
            Assert.That(options.Error, Is.False);
            Assert.That(options.Include == "1,2");
        }
        [Test]
        public void TestExcludeOption()
        {
            options.Parse("-exclude:1,2");
            Assert.That(options.Error, Is.False);
            Assert.That(options.Exclude == "1,2");
        }
        [Test]
        public void TestIncludeExcludeOption()
        {
            options.Parse("-include:3,4", "-exclude:1,2");
            Assert.That(options.Error, Is.False);
            Assert.That(options.Exclude == "1,2");
            Assert.That(options.Include == "3,4");
        }
#endif
    }
}
