using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MotionParsers {

    public static class MotionUtil {

        public static Vector4 LastOne = new Vector4(0, 0, 0, 1);

        public static Matrix4x4 Euler2Mat(float u, float v, float w) {
            float cu, cv, cw, su, sv, sw;
            cu = Mathf.Cos(u); cv = Mathf.Cos(v); cw = Mathf.Cos(w);
            su = Mathf.Sin(u); sv = Mathf.Sin(v); sw = Mathf.Sin(w);

            Vector4 r1 = new Vector4(cv*cw, su*sv*cw-cu*sw, su*sw+cu*sv*cw, 0);
            Vector4 r2 = new Vector4(cv*sw, cu*cw+su*sv*sw, cu*sv*sw-su*cw, 0);
            Vector4 r3 = new Vector4(-sv, su * cv, cu * cv, 0);

            return new Matrix4x4(r1, r2, r3, LastOne).transpose;
        }

        public static Matrix4x4 Euler2Mat(this Vector3 v) {
            return Euler2Mat(v.x, v.y, v.z);
        }

        public static T[] SubArray<T>(this T[] data, int index, int length) {
            int range = length - index;
            T[] result = new T[range];
            Array.Copy(data, index, result, 0, range);
            return result;
        }

        public static bool IsNumeric(this string s) {
            return int.TryParse(s, out int dummy);
        }

        public static Dictionary<T, V> DeepClone<T, V> (this Dictionary<T, V> orig) 
            where V : ICloneable
        {
            Dictionary<T, V> d = new Dictionary<T, V>(orig.Count, orig.Comparer);
            foreach (KeyValuePair<T, V> entry in orig)
            {
                d.Add(entry.Key, (V) entry.Value.Clone());
            }
            return d;
        }

        public static void Add<T, V>(this Dictionary<T, V> lhs, Dictionary<T, V> rhs)
            where V : IOperable<V>
        {
            //Dictionary<T, V> d = new Dictionary<T, V>(lhs.Count, lhs.Comparer);
            foreach (KeyValuePair<T, V> entry in lhs)
            {
                //V res = entry.Value.Add(rhs[entry.Key]);
                //d.Add(entry.Key, res);
                entry.Value.Add(rhs[entry.Key]);
            }
        }

        public static void Div<T, V>(this Dictionary<T, V> lhs, float divident)
            where V : IOperable<V>
        {
            //Dictionary<T, V> d = new Dictionary<T, V>(lhs.Count, lhs.Comparer);
            foreach (KeyValuePair<T, V> entry in lhs)
            {
                //V res = entry.Value.Div(divident);
                //d.Add(entry.Key, res);
                entry.Value.Div(divident);
            }
        }

        public static LinkedListNode<T> Pop<T>(this LinkedList<T> list)
        {
            LinkedListNode<T> node = list.First;
            list.RemoveFirst();
            return node;
        }

    }

    public interface IOperable<TRes>
    {
        TRes Add(TRes other);
        TRes Div(float divident);
    }

    public interface IMocapConverter
    {
        object Convert(object arg);
        Type GetConvertReturnType();
    }

}


