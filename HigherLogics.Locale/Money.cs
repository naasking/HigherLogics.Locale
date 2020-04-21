using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace HigherLogics.Locale
{
    /// <summary>
    /// A monetary value.
    /// </summary>
    /// <remarks>
    /// Inspired by: https://deque.blog/2017/08/17/a-study-of-4-money-class-designs-featuring-martin-fowler-kent-beck-and-ward-cunningham-implementations/
    /// </remarks>
    public readonly struct Money : IComparable<Money>, IEquatable<Money>
    {
        // this is a sorted map of numbers indexed by currency, in monototonically
        // increasing order, like a binary tree
        readonly Fixed<Currency>[] values;

        /// <summary>
        /// Construct a simple monetary value.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        public Money(decimal amount, Currency currency) : this(new[] { new Fixed<Currency>(amount, currency) })
        {
        }

        Money(params Fixed<Currency>[] values)
        {
            this.values = values;
        }

        /// <summary>
        /// Compare money values for equality.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Money other) =>
            values.SequenceEqual(other.values);

        /// <summary>
        /// Compare two monetary values.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Money other) =>
            values.Zip(other.values, ValueTuple.Create).Select(x => x.Item1.CompareTo(x.Item2)).First(x => x != 0);

        /// <summary>
        /// Add two values.
        /// </summary>
        /// <param name="lhs">The left hand value.</param>
        /// <param name="rhs">The right hand value.</param>
        /// <returns>The sum of the given values.</returns>
        public static Money operator +(Money lhs, Money rhs) =>
            new Money(Add(lhs.values, rhs.values));

        /// <summary>
        /// Subtract two values.
        /// </summary>
        /// <param name="lhs">The left hand value.</param>
        /// <param name="rhs">The right hand value.</param>
        /// <returns>The subtraction of the given values.</returns>
        public static Money operator -(Money lhs, Money rhs) =>
            new Money(Subtract(lhs.values, rhs.values));

        /// <summary>
        /// Multiply two values.
        /// </summary>
        /// <param name="lhs">The left hand value.</param>
        /// <param name="rhs">The right hand value.</param>
        /// <returns>The multiplication of the given values.</returns>
        public static Money operator *(Money lhs, decimal rhs) =>
            new Money(Multiply(lhs.values, rhs));

        /// <summary>
        /// Multiply two values.
        /// </summary>
        /// <param name="lhs">The left hand value.</param>
        /// <param name="rhs">The right hand value.</param>
        /// <returns>The multiplication of the given values.</returns>
        public static Money operator *(decimal lhs, Money rhs) =>
            new Money(Multiply(rhs.values, lhs));

        /// <summary>
        /// Divide two values.
        /// </summary>
        /// <param name="lhs">The left hand value.</param>
        /// <param name="rhs">The right hand value.</param>
        /// <returns>The division of the given values.</returns>
        public static Money operator /(Money lhs, decimal constant) =>
            new Money(Divide(lhs.values, constant));

        /// <summary>
        /// Negate a value.
        /// </summary>
        /// <param name="money">The value to negate.</param>
        /// <returns>The negation of the given values.</returns>
        public static Money operator -(Money money) =>
            -1 * money;

        #region Internal array ops
        static T[] Insert<T>(T[] source, int i, T item)
        {
            var arr = new T[source.Length + 1];
            Array.Copy(source, 0, arr, 0, i);
            arr[i] = item;
            Array.Copy(source, 0, arr, i + 1, source.Length - i);
            return arr;
        }

        static int Missing(Fixed<Currency>[] lhs, Fixed<Currency>[] rhs)
        {
            //FIXME: could just do left.Length + right.Length for simplicity and efficiency.
            //Or maybe, could use a heuristic based on the number of currency values because
            //those are always fixed at runtime
            // ie. scale factor = lhs.length / count(Currency) + rhs.Length/count(Currency)

            // find out how many entries are in one but not the other array
            var shorter = lhs.Length < rhs.Length ? lhs : rhs;
            var longer = shorter == lhs ? rhs : lhs;
            var missing = 0;
            for (int i = 0, j = 0; i < shorter.Length; ++i)
            {
                var x = shorter[i];
                j = Array.BinarySearch(longer, j, longer.Length, x);
                if (j < 0)
                {
                    ++missing;
                    j = ~j;
                }
            }
            return missing;
        }

        static Fixed<Currency>[] Add(Fixed<Currency>[] lhs, Fixed<Currency>[] rhs)
        {
            // given an array of appropriate size, traverse all arrays and copy the items in-order
            // so as to preserve the binary search invariant of monotonically increasing values
            var arr = new Fixed<Currency>[Missing(lhs, rhs) + lhs.Length];
            for (int i = 0, s = 0, l = 0; i < arr.Length; ++i)
            {
                switch (lhs[s].CompareTo(rhs[l]))
                {
                    case -1:
                        arr[i] = lhs[s++];
                        break;
                    case 0:
                        arr[i] = lhs[s++] + rhs[l++];
                        break;
                    case 1:
                        arr[i] = rhs[l++];
                        break;
                }
            }
            return arr;
        }

        static Fixed<Currency>[] Subtract(Fixed<Currency>[] lhs, Fixed<Currency>[] rhs)
        {
            // given an array of appropriate size, traverse all arrays and copy the items in-order
            // so as to preserve the binary search invariant of monotonically increasing values
            var arr = new Fixed<Currency>[Missing(lhs, rhs) + lhs.Length];
            for (int i = 0, l = 0, r = 0; i < arr.Length; ++i)
            {
                switch (lhs[l].CompareTo(rhs[r]))
                {
                    case -1:
                        arr[i] = lhs[l++];
                        break;
                    case 0:
                        arr[i] = lhs[l++] - rhs[r++];
                        break;
                    case 1:
                        arr[i] = rhs[r++];
                        break;
                }
            }
            return arr;
        }

        static Fixed<Currency>[] Multiply(Fixed<Currency>[] values, decimal constant)
        {
            var arr = new Fixed<Currency>[values.Length];
            for (int i = 0; i < arr.Length; ++i)
                arr[i] = values[i] * constant;
            return arr;
        }

        static Fixed<Currency>[] Divide(Fixed<Currency>[] values, decimal constant)
        {
            var arr = new Fixed<Currency>[values.Length];
            for (int i = 0; i < arr.Length; ++i)
                arr[i] = values[i] / constant;
            return arr;
        }
        #endregion
    }
}
