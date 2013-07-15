// ***********************************************************************
// Copyright (c) 2012 Charlie Poole
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
using System.Diagnostics;
using System.Threading;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Api;

namespace NUnit.Framework.Internal.WorkItems
{
    /// <summary>
    /// A WorkItem may be an individual test case, a fixture or
    /// a higher level grouping of tests. All WorkItems inherit
    /// from the abstract WorkItem class, which uses the template
    /// pattern to allow derived classes to perform work in
    /// whatever way is needed.
    /// </summary>
    public abstract class WorkItem
    {
        // The test this WorkItem represents
        private readonly Test _test;

        // The TestCommand for that test
        //private readonly TestCommand _command;

        // The execution context used by this work item
        private TestExecutionContext _context;

        // The current state of the WorkItem
        private WorkItemState _state;

        /// <summary>
        /// The result of running the test
        /// </summary>
        protected TestResult testResult;

        #region Static Factory Method
        
        /// <summary>
        /// Create a WorkItem appropriate for the test to be run
        /// </summary>
        /// <param name="test">The test to be executed</param>
        /// <param name="context">The execution context in which the test will be run</param>
        /// <param name="filter">A filter for selecting chind tests</param>
        /// <returns></returns>
        public static WorkItem CreateWorkItem(Test test, TestExecutionContext context, ITestFilter filter)
        {
            if (test.RunState != RunState.Runnable && test.RunState != RunState.Explicit)
                return new SimpleWorkItem(new SkipCommand(test), context);

            TestSuite suite = test as TestSuite;
            if (suite != null)
                return new CompositeWorkItem(suite, context, filter);

            return new SimpleWorkItem((TestMethod)test, context);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Construct a WorkItem for a particular test.
        /// </summary>
        /// <param name="test">The test that the WorkItem will run</param>
        /// <param name="context">The context to be used for running this test</param>
        public WorkItem(Test test, TestExecutionContext context)
        {
            _test = test;
            _context = context.Save();
            testResult = test.MakeTestResult();
            //_command = test.GetTestCommand();
            _state = WorkItemState.Ready;
        }

        #endregion

        #region Properties and Events

        /// <summary>
        /// Event triggered when the item is complete
        /// </summary>
        public event EventHandler Completed;

        /// <summary>
        /// Gets the current state of the WorkItem
        /// </summary>
        public WorkItemState State
        {
            get { return _state; }
        }

        /// <summary>
        /// The test being executed by the work item
        /// </summary>
        public Test Test
        {
            get { return _test; }
        }

        /// <summary>
        /// The execution context in use
        /// </summary>
        protected TestExecutionContext Context
        {
            get { return _context; }
        }

        /// <summary>
        /// The original context supplied from the fixture
        /// or other higher-level test
        /// </summary>
        protected TestExecutionContext PriorContext
        {
            get { return _context.prior; }
        }

        ///// <summary>
        ///// The command used to run the test
        ///// </summary>
        //protected TestCommand Command
        //{
        //    get { return _command; }
        //}

        /// <summary>
        /// The test result
        /// </summary>
        public TestResult Result
        {
            get { return testResult; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Execute the current work item, including any
        /// child work items.
        /// </summary>
        public virtual void Execute()
        {
#if (CLR_2_0 || CLR_4_0) && !NETCF && !SILVERLIGHT
            // Timeout set at a higher level
            int timeout = _context.TestCaseTimeout;

            // Timeout set on this test
            if (Test.Properties.ContainsKey(PropertyNames.Timeout))
                timeout = (int)Test.Properties.Get(PropertyNames.Timeout);

            if (Test is TestMethod && timeout > 0)
                RunTestWithTimeout(timeout);
            else
                RunTest();
#else
            RunTest();
#endif
        }

#if (CLR_2_0 || CLR_4_0) && !NETCF && !SILVERLIGHT
        private void RunTestWithTimeout(int timeout)
        {
            Thread thread = new Thread(new ThreadStart(RunTest));

            thread.Start();

            if (timeout <= 0)
                timeout = Timeout.Infinite;

            thread.Join(timeout);

            if (thread.IsAlive)
            {
                ThreadUtility.Kill(thread);

                // NOTE: Without the use of Join, there is a race condition here.
                // The thread sets the result to Cancelled and our code below sets
                // it to Failure. In order for the result to be shown as a failure,
                // we need to ensure that the following code executes after the
                // thread has terminated. There is a risk here: the test code might
                // refuse to terminate. However, it's more important to deal with
                // the normal rather than a pathological case.
                thread.Join();

                Result.SetResult(ResultState.Failure,
                    string.Format("Test exceeded Timeout value of {0}ms", timeout));

                WorkItemComplete();
            }
        }
#endif

        private void RunTest()
        {
            _context.CurrentTest = this.Test;
            _context.CurrentResult = this.Result;
            _context.Listener.TestStarted(this.Test);
            _context.StartTime = DateTime.Now;

            TestExecutionContext.SetCurrentContext(_context);

#if (CLR_2_0 || CLR_4_0) && !SILVERLIGHT && !NETCF_2_0
            long startTicks = Stopwatch.GetTimestamp();
#endif

            try
            {
                PerformWork();
            }
            finally
            {
#if (CLR_2_0 || CLR_4_0) && !SILVERLIGHT && !NETCF_2_0
                long tickCount = Stopwatch.GetTimestamp() - startTicks;
                double seconds = (double)tickCount / Stopwatch.Frequency;
                Result.Duration = TimeSpan.FromSeconds(seconds);
#else
                Result.Duration = DateTime.Now - Context.StartTime;
#endif

                Result.AssertCount = _context.AssertCount;

                _context.Listener.TestFinished(Result);

                _context = _context.Restore();
                _context.AssertCount += Result.AssertCount;
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Method that performs actually performs the work. It should
        /// set the State to WorkItemState.Complete when done.
        /// </summary>
        protected abstract void PerformWork();

        /// <summary>
        /// Method called by the derived class when all work is complete
        /// </summary>
        protected void WorkItemComplete()
        {
            _state = WorkItemState.Complete;
            if (Completed != null)
                Completed(this, EventArgs.Empty);
        }

        #endregion
    }
}
