using System;
using System.Collections.Generic;
using System.Text;

namespace HigherLogics
{
    /// <summary>
    /// A fixed precision numerical type.
    /// </summary>
    /// <typeparam name="T">The units for this number.</typeparam>
    readonly struct Fixed<T> : IEquatable<Fixed<T>>, IComparable<Fixed<T>>
        where T : struct
    {
        public Fixed(decimal value, T units)
        {
            Value = value;
            Units = units;
        }

        public decimal Value { get; }

        public T Units { get; }

        public bool Equals(Fixed<T> other) =>
            Value == other.Value && EqualityComparer<T>.Default.Equals(Units, other.Units);

        public int CompareTo(Fixed<T> other)
        {
            var x = Comparer<T>.Default.Compare(Units, other.Units);
            return x == 0 ? Value.CompareTo(other.Value) : x;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool Equals(object obj) =>
            obj is Fixed<T> f && Equals(f);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int GetHashCode() =>
            Value.GetHashCode();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override string ToString() =>
            Value.ToString();

        public static Fixed<T> operator /(Fixed<T> value, decimal constant) =>
            new Fixed<T>(value.Value / constant, value.Units);

        public static Fixed<T> operator *(Fixed<T> value, decimal constant) =>
            new Fixed<T>(value.Value * constant, value.Units);

        public static Fixed<T> operator *(decimal constant, Fixed<T> value) =>
            value * constant;

        public static Fixed<T> operator *(Fixed<T> lhs, Fixed<T> rhs) =>
            lhs * rhs.Value;

        public static Fixed<T> operator +(Fixed<T> lhs, Fixed<T> rhs) =>
            new Fixed<T>(lhs.Value + rhs.Value, lhs.Units);
        
        public static Fixed<T> operator -(Fixed<T> lhs, Fixed<T> rhs) =>
            new Fixed<T>(lhs.Value - rhs.Value, lhs.Units);
    }
}
