using KellermanSoftware.CompareNetObjects;
using System;

namespace Jira.FlowCharts
{
    /// <summary>
    /// A set of BDD style comparison extensions for use with Testing Frameworks
    /// </summary>
    public static class CompareExtensions
    {
        /// <summary>
        /// Throws a CompareException if the classes are not equal
        /// </summary>
        public static void AssertEqual<T>(this ICompareLogic compareLogic, T expected, T actual, string message = null)
        {
            ComparisonResult result = compareLogic.Compare(expected, actual);

            if (!result.AreEqual)
            {
                throw new CompareException(result, BuildExpectedEqualMessage(message, result));
            }
        }

        /// <summary>
        /// Throws a CompareException if the classes are equal
        /// </summary>
        public static void AssertNotEqual<T>(this ICompareLogic compareLogic, T expected, T actual, string message = null)
        {
            ComparisonResult result = compareLogic.Compare(expected, actual);

            if (result.AreEqual)
            {
                throw new CompareException(result, BuildExpectedNotEqualMessage(message, result));
            }
        }

        private static string BuildExpectedEqualMessage(string message, ComparisonResult result)
        {
            message = message ?? "Objects expected to be equal";
            return message + Environment.NewLine + result.DifferencesString + Environment.NewLine;
        }

        private static string BuildExpectedNotEqualMessage(string message, ComparisonResult result)
        {
            message = message ?? "Objects expected NOT to be equal";
            return message + Environment.NewLine + result.DifferencesString + Environment.NewLine;
        }
    }
}
