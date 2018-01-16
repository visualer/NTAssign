using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTAssign
{
    public enum AssignResult
    {
        accurate = 0,
        possible = 1,
        impossible = 2,
        error = 3
    }

    public static partial class Energy
    {
        public static int wRBM_min = 70, wRBM_max = 350;
        public static int[] sem = new int[] { 0, 1, 3, 4, 6, 7 };
        public static int[] met = new int[] { 2, 5, 8 };
        public static int nMin = 5;
        public static int nMax = 50;
        public static double maxEnergy = 5;
        public static int seriesThreshold = 90;
        public static string[] envArr = {
            "Air-suspended SWNTs", @"SWNTs on \(\mathrm{SiO_2}/\mathrm{Si}\) substrates", "SWNT arrays on quartz substrates",
            "\"Super-growth\" SWNTs", "SDS-dispersed SWNTs", "ssDNA-dispersed SWNTs"
        };
        public static double[,] param = new double[,]
                {
                    { 1.079, 0.077, 0.063 },
                    { 2.022, 0.308, 0.172 },
                    { 3.170, 0.764, 0.286 }, //M11
                    { 4.286, 1.230, 0.412 },
                    { 5.380, 1.922, 0.644 },
                    { 6.508, 2.768, 0.928 }, //M22
                    { 7.624, 3.768, 1.024 }, //S55
                    { 8.734, 4.921, 1.479 }, //S66
                    { 9.857, 6.228, 1.692 }, //M33
                };
        public static double[,] betap = new double[,]
                {
                    { 0.09, -0.07 },
                    { -0.18, 0.14 },
                    { -0.19, 0.29 },
                    { 0.49, -0.33 },
                    { -0.43, 0.59 },
                    { -0.6, 0.57 }
                };

        public static string[] pArr = {
            @"\(S_{11}\)", @"\(S_{22}\)", @"\(M_{11}^\pm\)", @"\(S_{33}\)", @"\(S_{44}\)", @"\(M_{22}^\pm\)", @"\(S_{55}\)", @"\(S_{66}\)", @"\(M_{33}^\pm\)"
        };

        public static string[] p1Arr = {
            @"\(S_{11}\)", @"\(S_{22}\)", @"\(M_{11}^-\)", @"\(M_{11}^+\)", @"\(S_{33}\)", @"\(S_{44}\)", @"\(M_{22}^-\)", @"\(M_{22}^+\)", @"\(S_{55}\)", @"\(S_{66}\)", @"\(M_{33}^-\)", @"\(M_{33}^+\)"
        };
        public static string[] p1Arr_raw = {
            "S_{11}", "S_{22}", "M_{11}^-", "M_{11}^+", "S_{33}", "S_{44}", "M_{22}^-", "M_{22}^+", "S_{55}", "S_{66}", "M_{33}^-", "M_{33}^+"
        };
        public static int[] p1ToP = { 0, 1, 2, 2, 3, 4, 5, 5, 6, 7, 8, 8 };
        public static int[] pToLesser = { 0, 0, 2, 3, 3, 5, 6, 6, 9 };
        public static int P1ToLesser(int p1) => p1 % 2 == 0 ? p1 : p1 - 1;
        public static double Dt(int n, int m, int type) => ((type != 4) ? 0.142 : 0.144) * Math.Sqrt(3 * (n * n + n * m + m * m)) / Math.PI;
        public static double Theta(int n, int m) => Math.Atan(Math.Sqrt(3) * m / (2 * n + m));
        public static int Mod(int n, int m) => (2 * n + m) % 3;
        public static bool IsMetal(int p) => (p + 1) % 3 == 0;
        public static bool IsMetal(int n, int m) => Mod(n, m) == 0;
        public static void GetRBMParameters(int p, int type, out double a, out double b)
        {
            switch (type)
            {
                case 0:
                    switch (p)
                    {
                        case 0:
                        case 1:
                            a = 204;
                            b = 27;
                            break;
                        case 2:
                            a = 200;
                            b = 26;
                            break;
                        default:
                            a = 228;
                            b = 0;
                            break;
                    }
                    break;
                case 1:
                    a = 235.9;
                    b = 5.5;
                    break;
                case 2:
                    a = 217.8;
                    b = 15.7;
                    break;
                case 3:
                    a = 227.0;
                    b = 0.3;
                    break;
                case 4:
                    a = 223.5;
                    b = 12.5;
                    break;
                case 5:
                    a = 218;
                    b = 18.3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", "invalid type");
            }
        }
        public static double RBMtoDt(double wRBM, int p, int type)
        {
            GetRBMParameters(p, type, out double a, out double b);
            return a / (wRBM - b);
        }
        public static double DttoRBM(double dt, int p, int type)
        {
            GetRBMParameters(p, type, out double a, out double b);
            return a / dt + b;
        }
        public static double GetEnergy_Sem(int n, int m, int p, int type) => GetEnergy(Dt(n, m, type), Theta(n, m), p, type, Mod(n, m));
        public static void Swap<T>(ref T a, ref T b)
        {
            T i = a;
            a = b;
            b = i;
        }
        /// <summary>
        /// 三阶收敛法求解f(x)=0
        /// </summary>
        /// <param name="f">函数f(x)=0</param>
        /// <param name="eps">收敛限</param>
        /// <param name="bounds">下界上界</param>
        /// <param name="x">根</param>
        /// <returns></returns>
        public static bool Solve(Func<decimal, decimal> f, decimal eps, decimal[] bounds, out decimal x)
        {
            int section = 100;
            for (int i = 1; i < section; i++)
            {
                decimal x1 = (bounds[1] - bounds[0]) / section * i + bounds[0], y, z;
                try
                {
                    do
                    {
                        x = x1;
                        z = 2 * f(x) * f(x) / (f(x + f(x)) - f(x - f(x)));
                        y = x - z;
                        x1 = x - z * (f(y) - f(x)) / (2 * f(y) - f(x));
                    } while (Math.Abs(x1 - x) > eps && x1 < bounds[1] && x1 > bounds[0]);
                }
                catch (Exception)
                {
                    continue;
                }
                if (Math.Abs(x1 - x) < eps)
                {
                    x = x1;
                    return true;
                }
            }
            x = -1;
            return false;
        }
    }
}