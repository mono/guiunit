using System;

namespace NUnit.Framework
{
	public partial class Assert
	{
		#region Greater

		#region int
		public static void Greater(int expected, int actual, string message, params object[] args)
		{
			Assert.That(expected, Is.GreaterThan (actual), message, args);
		}

		public static void Greater(int expected, int actual, string message)
		{
			Assert.That(expected, Is.GreaterThan (actual), message, null);
		}

		public static void Greater(int expected, int actual)
		{
			Assert.That(expected, Is.GreaterThan (actual));
		}
		#endregion

		#region double
		
		public static void Greater(double expected, double actual, string message, params object[] args)
		{
			Assert.That(expected, Is.GreaterThan (actual), message, args);
		}

		public static void Greater(double expected, double actual, string message)
		{
			Assert.That(expected, Is.GreaterThan (actual), message, null);
		}

		public static void Greater(double expected, double actual)
		{
			Assert.That(expected, Is.GreaterThan (actual));
		}
		#endregion

		#endregion

		#region Less

		#region int
		public static void Less(int expected, int actual, string message, params object[] args)
		{
			Assert.That(expected, Is.LessThan (actual), message, args);
		}

		public static void Less(int expected, int actual, string message)
		{
			Assert.That(expected, Is.LessThan (actual), message, null);
		}

		public static void Less(int expected, int actual)
		{
			Assert.That(expected, Is.LessThan (actual));
		}
		#endregion

		#region double

		public static void Less(double expected, double actual, string message, params object[] args)
		{
			Assert.That(expected, Is.LessThan (actual), message, args);
		}

		public static void Less(double expected, double actual, string message)
		{
			Assert.That(expected, Is.LessThan (actual), message, null);
		}

		public static void Less(double expected, double actual)
		{
			Assert.That(expected, Is.LessThan (actual));
		}
		#endregion

		#endregion

		#region IsInstanceOf

		public static void IsInstanceOf<T>(object actual, string message, params object[] args)
		{
			Assert.That(actual, Is.InstanceOf<T> (), message, args);
		}

		public static void IsInstanceOf<T>(object actual, string message)
		{
			Assert.That(actual, Is.InstanceOf<T> (), message, null);
		}

		public static void IsInstanceOf<T>(object actual)
		{
			Assert.That(actual, Is.InstanceOf<T> ());
		}

		#endregion
	}
}

