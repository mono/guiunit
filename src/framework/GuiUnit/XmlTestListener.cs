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
			if (result.Test.HasChildren)
				Write (new XElement ("suite-finished", new XAttribute ("name", result.Test.FullName), new XAttribute ("result", ToXmlString (result.ResultState))));
			else
				Write (new XElement ("test-finished", new XAttribute ("name", result.Test.FullName), new XAttribute ("result", ToXmlString (result.ResultState))));
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

