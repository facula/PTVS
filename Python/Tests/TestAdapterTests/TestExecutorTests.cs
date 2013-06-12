﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PythonTools.TestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace TestAdapterTests {
    [TestClass]
    public class TestExecutorTests {
        [ClassInitialize]
        public static void DoDeployment(TestContext context) {
            TestData.Deploy();
        }

        [TestMethod, Priority(0)]
        public void TestRun() {
            var executor = new TestExecutor();
            var recorder = new MockTestExecutionRecorder();
            var runContext = new MockRunContext();
            var expectedTests = TestInfo.TestAdapterATests.Concat(TestInfo.TestAdapterBTests).ToArray();
            var testCases = expectedTests.Select(tr => tr.TestCase);
            
            executor.RunTests(testCases, runContext, recorder);
            PrintTestResults(recorder.Results);

            foreach (var expectedResult in expectedTests) {
                var actualResult = recorder.Results.SingleOrDefault(tr => tr.TestCase.FullyQualifiedName == expectedResult.TestCase.FullyQualifiedName);

                Assert.IsNotNull(actualResult);
                Assert.AreEqual(expectedResult.Outcome, actualResult.Outcome);
            }
        }

        [TestMethod, Priority(0)]
        public void TestRunAll() {
            var executor = new TestExecutor();
            var recorder = new MockTestExecutionRecorder();
            var runContext = new MockRunContext();
            var expectedTests = TestInfo.TestAdapterATests.Concat(TestInfo.TestAdapterBTests).ToArray();

            executor.RunTests(new[] { TestInfo.TestAdapterLibProjectFilePath, TestInfo.TestAdapterAProjectFilePath, TestInfo.TestAdapterBProjectFilePath }, runContext, recorder);
            PrintTestResults(recorder.Results);

            foreach (var expectedResult in expectedTests) {
                var actualResult = recorder.Results.SingleOrDefault(tr => tr.TestCase.FullyQualifiedName == expectedResult.TestCase.FullyQualifiedName);

                Assert.IsNotNull(actualResult);
                Assert.AreEqual(expectedResult.Outcome, actualResult.Outcome);
            }
        }

        [TestMethod, Priority(0)]
        public void TestCancel() {
            var executor = new TestExecutor();
            var recorder = new MockTestExecutionRecorder();
            var runContext = new MockRunContext();
            var expectedTests = TestInfo.TestAdapterATests.Union(TestInfo.TestAdapterBTests).ToArray();
            var testCases = expectedTests.Select(tr => tr.TestCase);

            var thread = new System.Threading.Thread(o => {
                executor.RunTests(testCases, runContext, recorder);
            });
            thread.Start();

            // One of the tests being run is hard coded to take 10 secs
            Assert.IsTrue(thread.IsAlive);

            System.Threading.Thread.Sleep(100);

            executor.Cancel();
            System.Threading.Thread.Sleep(100);

            // It should take less than 10 secs to cancel
            // Depending on which assemblies are loaded, it may take some time
            // to obtain the interpreters service.
            Assert.IsTrue(thread.Join(10000));

            System.Threading.Thread.Sleep(100);

            Assert.IsFalse(thread.IsAlive);

            // Canceled test cases do not get recorded
            Assert.IsTrue(recorder.Results.Count < expectedTests.Length);
        }

        private static void PrintTestResults(IEnumerable<TestResult> results) {
            foreach (var result in results) {
                Debug.WriteLine("Test: " + result.TestCase.FullyQualifiedName);
                Debug.WriteLine("Result: " + result.Outcome);
                Debug.WriteLine("");
            }
        }
    }
}