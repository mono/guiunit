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

		#region IsInstanceOfType
		/// <summary>
		/// Verifies that the object that is passed in is equal to <code>null</code>
		/// If the object is not <code>null</code> then an <see cref="AssertionException"/>
		/// is thrown.
		/// </summary>
		/// <param name="anObject">The object that is to be tested</param>
		/// <param name="message">The message to display in case of failure</param>
		/// <param name="args">Array of objects to be used in formatting the message</param>
		public static void IsInstanceOfType (object anObject, string message, params object [] args)
		{
			Assert.That (anObject, Is.Null, message, args);
		}

		/// <summary>
		/// Asserts that an object is an instance of a given type.
		/// </summary>
		/// <param name="expected">The expected Type</param>
		/// <param name="actual">The object being examined</param>
		public static void IsInstanceOfType (System.Type expected, object actual)
		{
			IsInstanceOfType (expected, actual, string.Empty, null);
		}

		/// <summary>
		/// Asserts that an object is an instance of a given type.
		/// </summary>
		/// <param name="expected">The expected Type</param>
		/// <param name="actual">The object being examined</param>
		/// <param name="message">A message to display in case of failure</param>
		public static void IsInstanceOfType (System.Type expected, object actual, string message)
		{
			IsInstanceOfType (expected, actual, message, null);
		}

		/// <summary>
		/// Asserts that an object is an instance of a given type.
		/// </summary>
		/// <param name="expected">The expected Type</param>
		/// <param name="actual">The object being examined</param>
		/// <param name="message">A message to display in case of failure</param>
		/// <param name="args">An array of objects to be used in formatting the message</param>
		public static void IsInstanceOfType (System.Type expected, object actual, string message, params object [] args)
		{
			Assert.That (actual.GetType (), Is.EqualTo (expected), message, args);
		}
		#endregion

		#endregion
	}
}

