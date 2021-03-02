using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class Complex
    {
        public static Complex I   = new Complex(0.0, 1.0);
        public static Complex IPI = new Complex(0.0, Math.PI);

        private double re, im;

        public float Re32
        {
            get { return Convert.ToSingle(re); }
        }

        public float Im32
        {
            get { return Convert.ToSingle(im); }
        }

        public double Re
        {
            get { return re; }
        }

        public double Im
        {
            get { return im; }
        }

        public Complex Conjugate
        {
            get { return Mathc.Conjugate(this); }
        }

        public Complex() { }

        public Complex(double re, double im)
        {
            this.re = re;
            this.im = im;
        }

        public Complex(float re, float im) : this((double)re, (double)im) { }


        /**********************************************************/
        /*    STATIC METHODS AND OPERATORS (and some overrides)   */
        /**********************************************************/

        public static implicit operator double(Complex z) => z.Re; 
        public static implicit operator Complex(double s) => new Complex(s, 0.0);

        public static Complex operator +(Complex lhs, Complex rhs)
        {
            return new Complex
            {
                re = lhs.re + rhs.re,
                im = lhs.im + rhs.im
            };
        }

        public static Complex operator +(Complex lhs, double rhs)
        {
            return lhs + new Complex(rhs, 0.0);
        }

        public static Complex operator +(double lhs, Complex rhs)
        {
            return rhs + lhs;
        }

        public static Complex operator -(Complex lhs, Complex rhs)
        {
            return new Complex
            {
                re = lhs.re - rhs.re,
                im = lhs.im - rhs.im
            };
        }

        public static Complex operator -(Complex lhs, double rhs)
        {
            return lhs - new Complex(rhs, 0.0);
        }

        public static Complex operator -(double lhs, Complex rhs)
        {
            return new Complex(lhs, 0.0) - rhs;
        }

        public static Complex operator *(Complex lhs, Complex rhs)
        {
            return new Complex
            {
                re = lhs.re * rhs.re - lhs.im * rhs.im,
                im = lhs.re * rhs.im + lhs.im * rhs.re
            };
        }

        public static Complex operator *(Complex lhs, double rhs)
        {
            return new Complex
            {
                re = lhs.re * rhs,
                im = lhs.im * rhs
            };
        }

        public static Complex operator *(double lhs, Complex rhs)
        {
            return rhs * lhs;
        }

        public static Complex operator /(Complex lhs, Complex rhs)
        {
            Complex zbar = rhs.Conjugate;
            Complex numer = lhs * zbar;
            Complex denom = rhs * zbar;
            return new Complex
            {
                re = numer.re / denom.re,
                im = numer.im / denom.re
            };
        }

        public static Complex operator /(double lhs, Complex rhs)
        {
            return new Complex(lhs, 0.0) / rhs;
        }

        public static Complex operator /(Complex lhs, double rhs)
        {
            return new Complex
            {
                re = lhs.re / rhs,
                im = lhs.im / rhs
            };
        }

        public override string ToString()
        {
            string sgn = im >= 0.0 ? "+" : "-";
            const string decformat = "{0:0.####} {1} {2:0.####}i";
            return string.Format(decformat, re, sgn, Math.Abs(im));
        }
    }

    public static class Mathc
    {
        public static Complex Sqrt(double s)
        {
            if (s >= 0.0)
                return new Complex(Math.Sqrt(s), 0.0);

            return new Complex(0.0, Math.Sqrt(Math.Abs(s)));
        }

        public static Complex Pow(Complex z, int p)
        {
            double r = Abs(z);
            double theta = Arg(z);
            return Math.Pow(r, p) * Exp(new Complex(0.0, (double)p * theta));
        }

        public static Complex Pow(Complex z, Complex p)
        {
            double r = Abs(z);
            Complex ztheta = new Complex(0.0, Arg(z));
            return Exp(Math.Log(r) * p + ztheta * p);
        }

        public static Complex Conjugate(Complex z)
        {
            return new Complex(z.Re, -z.Im);
        }

        public static double Arg(Complex z)
        {
            return Math.Atan2(z.Im, z.Re);
        }

        public static double Abs(Complex z)
        {
            return Math.Sqrt(z.Re * z.Re + z.Im * z.Im);
        }

        public static Complex Exp(Complex z)
        {
            if (Math.Abs(z.Re) < 1e-5)
                return new Complex(Math.Cos(z.Im), Math.Sin(z.Im));

            return Math.Exp(z.Re) * new Complex(Math.Cos(z.Im), Math.Sin(z.Im));
        }

    }

}



