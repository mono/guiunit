// ***********************************************************************
// Copyright (c) 2007 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Filters;
using System.Diagnostics;
using NUnitLite.Runner;

namespace MacUnit
{
	/// <summary>
	/// TextUI is a general purpose class that runs tests and
	/// outputs to a TextWriter.
	/// 
	/// Call it from your Main like this:
	///   new TextUI(textWriter).Execute(args);
	///     OR
	///   new TextUI().Execute(args);
	/// The provided TextWriter is used by default, unless the
	/// arguments to Execute override it using -out. The second
	/// form uses the Console, provided it exists on the platform.
	/// 
	/// NOTE: When running on a platform without a Console, such
	/// as Windows Phone, the results will simply not appear if
	/// you fail to specify a file in the call itself or as an option.
	/// </summary>
	public class MacTestRunner : ITestListener
	{
		public static int Main (string[] args)
		{
			Environment.ExitCode = 1;
			for (int i = 0; i  <args.Length; i ++) {
				if (args [i].Contains ("-xml"))
					args [i] = args [i].Replace ("-xml", "-result");
				if (args [i].Contains ("-run")) {
					args [i] = args [i].Replace ("-run", "-test");
				}
			}
			new MacTestRunner ().Execute (args);
			return 0;
		}

		private CommandLineOptions commandLineOptions;

		private NUnit.ObjectList assemblies = new NUnit.ObjectList();

		private TextWriter writer;

		private ITestListener listener;

		private ITestAssemblyRunner runner;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TextUI"/> class.
		/// </summary>
		public MacTestRunner() : this(ConsoleWriter.Out, TestListener.NULL) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextUI"/> class.
		/// </summary>
		/// <param name="writer">The TextWriter to use.</param>
		public MacTestRunner(TextWriter writer) : this(writer, TestListener.NULL) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextUI"/> class.
		/// </summary>
		/// <param name="writer">The TextWriter to use.</param>
		/// <param name="listener">The Test listener to use.</param>
		public MacTestRunner(TextWriter writer, ITestListener listener)
		{
			// Set the default writer - may be overridden by the args specified
			this.writer = writer;
			this.runner = new NUnitLiteTestAssemblyRunner(new NUnitLiteTestAssemblyBuilder());
			this.listener = listener;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Execute a test run based on the aruments passed
		/// from Main.
		/// </summary>
		/// <param name="args">An array of arguments</param>
		public void Execute(string[] args)
		{
			ExecuteCore (args);
		}

		bool ExecuteOnXwt (Action action)
		{
			try {
				// Firstly init Xwt
				var application = Type.GetType ("Xwt.Application, Xwt");
				foreach (var impl in new [] { "Xwt.Gtk.dll", "Xwt.Mac.dll", "Xwt.Wpf.dll"}) {
					var xwtImpl = Path.Combine (Path.GetDirectoryName (application.Assembly.Location), impl);
					if (File.Exists (xwtImpl))
						Assembly.LoadFile (xwtImpl);
				}

				var initMethods = application.GetMethods (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				var initMethod = initMethods.First (m => m.Name == "Initialize" && m.GetParameters ().Length == 1 && m.GetParameters () [0].ParameterType == typeof(string));
				initMethod.Invoke (null, new [] { "Xwt.GtkBackend.GtkEngine, Xwt.Gtk" });

				System.Threading.ThreadPool.QueueUserWorkItem (d => {
					action ();
					MainLoopHelper.ExecuteOnMainThread (() => {
						application.GetMethod ("Exit").Invoke (null, null);
						return null;
					});
				});
				
				application.GetMethod ("Run").Invoke (null, null);
				return true;
			} catch {
				return false;
			}
		}

		bool ExecuteOnMonoMac (Action action)
		{
			try {
				// Firstly init monomac
				var nsapp = Type.GetType ("MonoMac.AppKit.NSApplication, MonoMac");
				var initMethod = nsapp.GetMethod ("Init", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				initMethod.Invoke (null, null);

				// Next get the shared application so we can invoke some methods on it
				var prop = nsapp.GetProperty ("SharedApplication", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				var sharedApplication = prop.GetValue (null, null);

				System.Threading.ThreadPool.QueueUserWorkItem (d => {
					action ();
					MainLoopHelper.ExecuteOnMainThread (() => {
						nsapp.GetMethod ("Terminate").Invoke (sharedApplication, new [] { sharedApplication });
						return null;
					});
				});

				nsapp.GetMethod ("Run").Invoke (sharedApplication, null);
				return true;
			} catch {
				return false;
			}
		}

		void ExecuteCore(string[] args)
		{
			// NOTE: Execute must be directly called from the
			// test assembly in order for the mechanism to work.
			Assembly callingAssembly = Assembly.GetCallingAssembly();

			this.commandLineOptions = new CommandLineOptions();
			commandLineOptions.Parse(args);

			if (commandLineOptions.OutFile != null)
				this.writer = new StreamWriter(commandLineOptions.OutFile);

			if (!commandLineOptions.NoHeader)
				WriteHeader(this.writer);

			if (commandLineOptions.ShowHelp)
				writer.Write(commandLineOptions.HelpText);
			else if (commandLineOptions.Error)
			{
				writer.WriteLine(commandLineOptions.ErrorMessage);
				writer.WriteLine(commandLineOptions.HelpText);
			}
			else
			{
				WriteRuntimeEnvironment(this.writer);

				if (commandLineOptions.Wait && commandLineOptions.OutFile != null)
					writer.WriteLine("Ignoring /wait option - only valid for Console");

				#if SILVERLIGHT
				IDictionary loadOptions = new System.Collections.Generic.Dictionary<string, string>();
				#else
				IDictionary loadOptions = new Hashtable();
				#endif
				//if (options.Load.Count > 0)
				//    loadOptions["LOAD"] = options.Load;

				//IDictionary runOptions = new Hashtable();
				//if (commandLineOptions.TestCount > 0)
				//    runOptions["RUN"] = commandLineOptions.Tests;

				ITestFilter filter = commandLineOptions.TestCount > 0
					? new SimpleNameFilter(commandLineOptions.Tests)
						: TestFilter.Empty;

				try
				{
					foreach (string name in commandLineOptions.Parameters)
						assemblies.Add(Assembly.LoadFile(name));

					if (assemblies.Count == 0)
						assemblies.Add(callingAssembly);

					// TODO: For now, ignore all but first assembly
					Assembly assembly = assemblies[0] as Assembly;

					if (!runner.Load(assembly, loadOptions))
					{
						AssemblyName assemblyName = AssemblyHelper.GetAssemblyName(assembly);
						Console.WriteLine("No tests found in assembly {0}", assemblyName.Name);
						return;
					}

					if (commandLineOptions.Explore)
						ExploreTests();
					else
					{
						if (commandLineOptions.Include != null && commandLineOptions.Include != string.Empty)
						{
							TestFilter includeFilter = new SimpleCategoryExpression(commandLineOptions.Include).Filter;

							if (filter.IsEmpty)
								filter = includeFilter;
							else
								filter = new AndFilter(filter, includeFilter);
						}

						if (commandLineOptions.Exclude != null && commandLineOptions.Exclude != string.Empty)
						{
							TestFilter excludeFilter = new NotFilter(new SimpleCategoryExpression(commandLineOptions.Exclude).Filter);

							if (filter.IsEmpty)
								filter = excludeFilter;
							else if (filter is AndFilter)
								((AndFilter)filter).Add(excludeFilter);
							else
								filter = new AndFilter(filter, excludeFilter);
						}
						if (ExecuteOnXwt (() => RunTests (filter))) {

						} else if (ExecuteOnMonoMac (() => RunTests (filter))) {

						} else {
							throw new Exception ("Unsupported Toolkit");
						}
					}
				}
				catch (FileNotFoundException ex)
				{
					writer.WriteLine(ex.Message);
				}
				catch (Exception ex)
				{
					writer.WriteLine(ex.ToString());
				}
				finally
				{
					if (commandLineOptions.OutFile == null)
					{
						if (commandLineOptions.Wait)
						{
							Console.WriteLine("Press Enter key to continue . . .");
							Console.ReadLine();
						}
					}
					else
					{
						writer.Close();
					}
				}
			}
		}

		/// <summary>
		/// Write the standard header information to a TextWriter.
		/// </summary>
		/// <param name="writer">The TextWriter to use</param>
		public static void WriteHeader(TextWriter writer)
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			#if NUNITLITE
			string title = "NUnitLite";
			#else
			string title = "NUNit Framework";
			#endif
			AssemblyName assemblyName = AssemblyHelper.GetAssemblyName(executingAssembly);
			System.Version version = assemblyName.Version;
			string copyright = "Copyright (C) 2012, Charlie Poole";
			string build = "";

			object[] attrs = executingAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (attrs.Length > 0)
			{
				AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute)attrs[0];
				title = titleAttr.Title;
			}

			attrs = executingAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
			if (attrs.Length > 0)
			{
				AssemblyCopyrightAttribute copyrightAttr = (AssemblyCopyrightAttribute)attrs[0];
				copyright = copyrightAttr.Copyright;
			}

			attrs = executingAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
			if (attrs.Length > 0)
			{
				AssemblyConfigurationAttribute configAttr = (AssemblyConfigurationAttribute)attrs[0];
				if (configAttr.Configuration.Length > 0)
					build = string.Format("({0})", configAttr.Configuration);
			}

			writer.WriteLine(String.Format("{0} {1} {2}", title, version.ToString(3), build));
			writer.WriteLine(copyright);
			writer.WriteLine();
		}

		/// <summary>
		/// Write information about the current runtime environment
		/// </summary>
		/// <param name="writer">The TextWriter to be used</param>
		public static void WriteRuntimeEnvironment(TextWriter writer)
		{
			string clrPlatform = Type.GetType("Mono.Runtime", false) == null ? ".NET" : "Mono";

			writer.WriteLine("Runtime Environment -");
			writer.WriteLine("    OS Version: {0}", Environment.OSVersion);
			writer.WriteLine("  {0} Version: {1}", clrPlatform, Environment.Version);
			writer.WriteLine();
		}

		#endregion

		#region Helper Methods

		private void RunTests(ITestFilter filter)
		{
			ITestResult result = runner.Run(this, filter);
			new ResultReporter(result, writer).ReportResults();
			if (commandLineOptions.ResultFile != null)
			{
				new NUnit2XmlOutputWriter().WriteResultFile (result, commandLineOptions.ResultFile);

				Console.WriteLine();
				Console.WriteLine("Results saved as {0}.", commandLineOptions.ResultFile);
			}
		}

		private void ExploreTests()
		{
			XmlNode testNode = runner.LoadedTest.ToXml(true);

			string listFile = commandLineOptions.ExploreFile;
			TextWriter textWriter = listFile != null && listFile.Length > 0
				? new StreamWriter(listFile)
					: Console.Out;

			#if CLR_2_0 || CLR_4_0
			System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
			settings.Indent = true;
			settings.Encoding = System.Text.Encoding.UTF8;
			System.Xml.XmlWriter testWriter = System.Xml.XmlWriter.Create(textWriter, settings);
			#else
			System.Xml.XmlTextWriter testWriter = new System.Xml.XmlTextWriter(textWriter);
			testWriter.Formatting = System.Xml.Formatting.Indented;
			#endif

			testNode.WriteTo(testWriter);
			testWriter.Close();

			Console.WriteLine();
			Console.WriteLine("Test info saved as {0}.", listFile);
		}

		#endregion

		#region ITestListener Members

		/// <summary>
		/// A test has just started
		/// </summary>
		/// <param name="test">The test</param>
		public void TestStarted(ITest test)
		{
			if (commandLineOptions.LabelTestsInOutput)
				writer.WriteLine("***** {0}", test.Name);
		}

		/// <summary>
		/// A test has just finished
		/// </summary>
		/// <param name="result">The result of the test</param>
		public void TestFinished(ITestResult result)
		{
		}

		/// <summary>
		/// A test has produced some text output
		/// </summary>
		/// <param name="testOutput">A TestOutput object holding the text that was written</param>
		public void TestOutput(TestOutput testOutput)
		{
		}

		#endregion
	}
}
