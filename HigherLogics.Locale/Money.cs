using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace HigherLogics.Locale
{
    /// <summary>
    /// A monetary value.
    /// </summary>
    /// <typeparam name="Currency">The kind of the currency type.</typeparam>
    /// <remarks>
    /// Inspired by: https://deque.blog/2017/08/17/a-study-of-4-money-class-designs-featuring-martin-fowler-kent-beck-and-ward-cunningham-implementations/
    /// </remarks>
    public readonly struct Money : IComparable<Money>, IEquatable<Money>
    {
        // when sum == null, amount and currency are valid.
        // when sum != null, amount and currency are invalid.
        readonly decimal amount;
        readonly Currency currency;
        readonly List<Money> sum;

        /// <summary>
        /// Construct a simple monetary value.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        public Money(decimal amount, Currency currency)
        {
            this.amount = amount;
            this.currency = currency;
            this.sum = null;
        }
        
        Money(List<Money> sum)
        {
            Debug.Assert(sum.All(x => x.sum == null));

            // ensure all monetary values are ordered consistently so equality works
            sum.Sort();
            this.sum = sum;
            this.amount = 0;
            this.currency = default(Currency);
        }

        /// <summary>
        /// Compare money values for equality.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Money other)
        {
            // monetary values must either be:
            // 1. simple values with equal amounts and equal currencies
            // 2. sums with the same sequence of sub-values, in the same order
            return sum == null && other.sum == null && amount == other.amount && currency.CompareTo(other.currency) == 0
                || sum != null && other.sum != null && PairwiseEqual(sum, other.sum);
        }

        static bool PairwiseEqual(List<Money> left, List<Money> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; ++i)
                if (!left[i].Equals(right[i]))
                    return false;
            return true;
        }

        static int PairwiseCompare(List<Money> left, List<Money> right)
        {
            for (int i = 0; i < Math.Min(left.Count, right.Count); ++i)
            {
                var x = left[i].CompareTo(right[i]);
                if (x != 0)
                    return x;
            }
            return 0;
        }

        /// <summary>
        /// Compare two monetary values.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Money other)
        {
            if (sum == null && other.sum == null)
            {
                var x = currency.CompareTo(other.currency);
                return x == 0 ? amount.CompareTo(other.amount) : x;
            }
            else if (other.sum == null)
            {
                return 1; // simple values should appear before sums
            }
            else
            {
                // generates a lazy sequence of (0 | positive | negative), so we just return the first non-zero
                // if no non-zero, this returns 0 anyway, which indicates equality
                return PairwiseCompare(sum, other.sum);
            }
        }

        /// <summary>
        /// Return the decimal amount for the given currency.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="xchg"></param>
        /// <returns></returns>
        public decimal Amount(Currency target, Func<Currency, Currency, decimal> xchg)
        {
            if (xchg == null)
                throw new ArgumentNullException("xchg");
            return sum == null ? xchg(currency, target) * amount:
                                 sum.Sum(x => x.Amount(target, xchg));
        }

        /// <summary>
        /// Multiply the monetary value by a constant.
        /// </summary>
        /// <param name="money"></param>
        /// <param name="constant"></param>
        /// <returns></returns>
        public static Money operator *(Money money, decimal constant)
        {
            return money.sum == null
                 ? new Money(money.amount * constant, money.currency)
                 : new Money(money.sum.Select(x => x * constant).ToList());
        }

        /// <summary>
        /// Multiply the monetary value by a constant.
        /// </summary>
        /// <param name="money"></param>
        /// <param name="constant"></param>
        /// <returns></returns>
        public static Money operator *(decimal constant, Money money)
        {
            return money * constant;
        }
        
        /// <summary>
        /// Add two monetary values.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Money operator +(Money left, Money right)
        {
            // flatten any embedded sums to ensure top-level monetary expressions only contain flat arrays of values
            return left.sum != null && right.sum != null ? new Money(left.sum.Concat(right.sum).ToList()):
                   left.sum != null                      ? new Money(Append(left.sum, right)):
                   right.sum != null                     ? new Money(Append(right.sum, left)):
                                                           new Money(new List<Money> { left, right });
        }

        static List<T> Append<T>(List<T> source, T item)
        {
            var list = new List<T>(source);
            list.Add(item);
            return list;
        }

        /// <summary>
        /// Add two monetary values.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Money operator -(Money left, Money right)
        {
            return left + -right;
        }

        /// <summary>
        /// Negate a monetary value.
        /// </summary>
        /// <param name="money"></param>
        /// <returns></returns>
        public static Money operator -(Money money)
        {
            return money.sum == null
                 ? new Money(-money.amount, money.currency)
                 : new Money(money.sum.Select(x => -x).ToList());
        }
    }
}
