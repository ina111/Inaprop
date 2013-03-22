using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Inaprop
{
    /// <summary>
    /// Spline interpolation class.
    /// スプライン補間のためのクラス。
    /// 使い方：下のSampleMainの中にある通り。
    /// var known = new Dictionary<double, double>でx軸をKey,y軸をValueとして指定。
    /// start,end,stepをknownのkeyのknown.First().Key, known.Last().Key, (end - star) / 個数 で指定
    /// for (var x = start; x<= end; x += step){
    ///     var y = scaler.GetValue(x);}
    /// ソース元：https://gist.github.com/3526685
    /// </summary>
    public class SplineInterpolator
    {
        private readonly double[] _keys;
        private readonly double[] _values;
        private readonly double[] _h;
        private readonly double[] _a;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="nodes">わかっているポイントのCollectionは補間のために
        /// 2つ以上のitemを持たなくてはいけない</param>
        public SplineInterpolator(IDictionary<double, double> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException("nodes");
            }

            var n = nodes.Count;

            if (n < 2)
            {
                throw new ArgumentException("At least two point required for interpolation.");
            }

            _keys = nodes.Keys.ToArray();
            _values = nodes.Values.ToArray();
            _a = new double[n];
            _h = new double[n];

            for (int i = 1; i < n; i++)
            {
                _h[i] = _keys[i] - _keys[i - 1];
            }

            if (n > 2)
            {
                var sub = new double[n - 1];
                var diag = new double[n - 1];
                var sup = new double[n - 1];

                for (int i = 1; i <= n - 2; i++)
                {
                    diag[i] = (_h[i] + _h[i + 1]) / 3;
                    sup[i] = _h[i + 1] / 6;
                    sub[i] = _h[i] / 6;
                    _a[i] = (_values[i + 1] - _values[i]) / _h[i + 1] - (_values[i] - _values[i - 1]) / _h[i];
                }

                SolveTridiag(sub, diag, sup, ref _a, n - 2);
            }
        }

        /// <summary>
        /// Gets interpolated value for specified argument.
        /// </summary>
        /// <param name="key">Argument value for interpolation. Must be within 
        /// the interval bounded by lowest ang highest <see cref="_keys"/> values.</param>
        public double GetValue(double key)
        {
            int gap = 0;
            var previous = double.MinValue;

            // At the end of this iteration, "gap" will contain the index of the interval
            // between two known values, which contains the unknown z, and "previous" will
            // contain the biggest z value among the known samples, left of the unknown z
            for (int i = 0; i < _keys.Length; i++)
            {
                if (_keys[i] < key && _keys[i] > previous)
                {
                    previous = _keys[i];
                    gap = i + 1;
                }
            }

            var x1 = key - previous;
            var x2 = _h[gap] - x1;

            return ((-_a[gap - 1] / 6 * (x2 + _h[gap]) * x1 + _values[gap - 1]) * x2 +
                (-_a[gap] / 6 * (x1 + _h[gap]) * x2 + _values[gap]) * x1) / _h[gap];
        }

        /// <summary>
        /// Solve linear system with tridiagonal n*n matrix "a"
        /// using Gaussian elimination without pivoting.
        /// </summary>
        private static void SolveTridiag(double[] sub, double[] diag, double[] sup, ref double[] b, int n)
        {
            int i;

            for (i = 2; i <= n; i++)
            {
                sub[i] = sub[i] / diag[i - 1];
                diag[i] = diag[i] - sub[i] * sup[i - 1];
                b[i] = b[i] - sub[i] * b[i - 1];
            }

            b[n] = b[n] / diag[n];

            for (i = n - 1; i >= 1; i--)
            {
                b[i] = (b[i] - sup[i] * b[i + 1]) / diag[i];
            }
        }

        /// <summary>
        /// 自作メソッド。線形補間。gapに
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double GetLinearValue(double key)
        {
            int gap = 0;
            double previousValue = double.MinValue;

            for (int i = 0; i < _keys.Length; i++)
            {
                if (_keys[i] < key && _keys[i] > previousValue)
                {
                    previousValue = _keys[i];
                    gap = i + 1;
                }
            }

            var delta_x = _keys[gap] - _keys[gap - 1];
            var delta_y = _values[gap] - _values[gap - 1];
            //y = y0 + (x - x0) * (y1 - y0) / (x1 -x0)
            return _values[gap - 1] + delta_y / delta_x * (key - _keys[gap - 1]);
        }
    }

    class Program
    {
        static void SampleMain(string[] args)
        {
            var r = 1000;
            var known = new Dictionary<double, double>
                { 
                    { 0.0, 0.0 },
                    { 100.0, 0.50 * r },
                    { 300.0, 0.75 * r },
                    { 500.0, 1.00 * r },
                };

            foreach (var pair in known)
            {
                Debug.WriteLine(String.Format("{0:0.000}\t{1:0.000}", pair.Key, pair.Value));
            }

            var scaler = new SplineInterpolator(known);
            var start = known.First().Key;
            var end = known.Last().Key;
            var step = (end - start) / 50;

            for (var x = start; x <= end; x += step)
            {
                var y = scaler.GetValue(x);
                Debug.WriteLine(String.Format("\t\t{0:0.000}\t{1:0.000}", x, y));
            }
        }
    }
}
