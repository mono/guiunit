using System;
using NUnit.Framework.Api;
using System.Xml.Linq;
using System.IO;

namespace GuiUnit
{
	public class XmlTestListener : ITestListener
	{
		TextWriter Writer {
			get; set;
		}

		public XmlTestListener (TextWriter writer)
		{
			Writer = writer;
		}

		public void TestStarted (ITest test)
		{
			if (test.HasChildren)
				Write (new XElement ("suite-started", new XAttribute ("name", test.FullName)));
			else
				Write (new XElement ("test-started", new XAttribute ("name", test.FullName)));
		}

		public void TestFinished (ITestResult result)
		{
			var element = new XElement (result.Test.HasChildren ? "suite-finished" : "test-finished",
			                            new XAttribute ("name", result.Test.FullName),
			                            new XAttribute ("result", ToXmlString (result.ResultState)),
			                            new XAttribute ("passed", result.PassCount),
			                            new XAttribute ("failures", result.FailCount),
			                            new XAttribute ("ignored", result.SkipCount),
			                            new XAttribute ("inconclusive", result.InconclusiveCount)
			);
			Write (element);
		}

		public void TestOutput (TestOutput testOutput)
		{
			// Ignore
		}

		object ToXmlString (ResultState resultState)
		{
			if (resultState == ResultState.Success)
				return "Success";
			else if (resultState == ResultState.Inconclusive)
				return "Inconclusive";
			else if (resultState == ResultState.Ignored)
				return "Ignored";
			else
				return "Failure";
		}

		void Write (XElement element)
		{
			try {
				Writer.WriteLine (element.ToString ());
			} catch {
			}
		}
	}
}

