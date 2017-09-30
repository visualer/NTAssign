using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTAssign
{
    public static partial class Energy
    {

        /// <summary>
        /// 返回对应能量。
        /// </summary>
        /// <param name="p"></param>
        /// <param name="type">0: Air-suspended, 1: SiO2/Si, 2: Quartz, 3: Super-growth, 4: SDS-disp., 5: ssDNA-disp.</param>
        /// <param name="mod">-1: Mii-, 0: Mii+, 1, MOD1, 2 MOD2</param>
        /// <returns></returns>
        public static double GetEnergy(double dt, double theta, int p, int type, int mod) => GetEnergy_Cos3Theta(dt, Math.Cos(3 * theta), p, type, mod);
        
        public static double GetEnergy_Cos3Theta(double dt, double cos3Theta, int p, int type, int mod)
        {
            // Get the correlated energy
            // dt: diam, theta: chiral angle
            // p: p desc. in LKH
            // type: 0: Air-suspended, 1: SiO2/Si, 2: Quartz, 3: Super-growth, 4: SDS-disp., 5: ssDNA-disp.
            double r;
            if (IsMetal(p) && mod > 0)
                throw new ArgumentException("mod should be in accordance with p");
            if (type <= 2)
            {
                if (p >= 9)
                    throw new ArgumentOutOfRangeException("higher than S66 not available");
                double deriative = -param[p, 0] / (dt * dt) + 2 * param[p, 1] / (dt * dt * dt);
                if (deriative > 0) // 1st deriative
                    throw new ArgumentOutOfRangeException("dt", "dt is too small" + deriative);
                if (IsMetal(p))
                    r = param[p, 0] / dt - param[p, 1] / (dt * dt) + param[p, 2] / (dt * dt) * cos3Theta * (mod * 2 + 1);
                // mod * 2 + 1 <==> mod == 0 ? 1 : -1
                else
                    r = param[p, 0] / dt - param[p, 1] / (dt * dt) +
                        param[p, 2] / (dt * dt) * cos3Theta * (((p % 3) == (mod % 2)) ? -1 : 1);
                r -= (type == 1) ? 0.04 : ((type == 2) ? 0.1 : 0);
            }
            else if (type == 3)
            {
                
                if (p >= 6)
                    throw new ArgumentOutOfRangeException("p", "higher than M22 not available for Super-Growth");
                double a = 1.074, b = 0.467, c = 0.812;
                double sgE(int extmod) => a * (p + 1) / dt * (1 + b * Math.Log10(c / ((p + 1) / dt)))
                        + betap[p, extmod] / (dt * dt) * cos3Theta
                        + ((p > 2) ? 0.059 * (p + 1) / dt : 0); // extra for larger than M11; warning: p + 1
                if (IsMetal(p))
                    r = sgE(mod + 1); // 0(Mii+) -> 1, -1(Mii-) -> 0
                else
                    r = sgE(mod - 1); // 1(MOD1) -> 0, 2(MOD2) -> 1
            }
            else if (type == 4 || type == 5)
            {
                if (p == 0)
                    r = 1 / (0.1270 + 0.8606 * dt) + ((mod == 1) ? 0.04575 : -0.08802) / (dt * dt) * cos3Theta;
                else if (p == 1)
                    r = 1 / (0.1174 + 0.4644 * dt) + ((mod == 1) ? -0.1829 : 0.1705) / (dt * dt) * cos3Theta;
                else
                    throw new ArgumentOutOfRangeException("p", "only S11 and S22 are available for SDS-disp. or ssDNA disp.");
                r -= (type == 5) ? 0.02 : 0;
            }
            else
                throw new ArgumentOutOfRangeException("type", "invalid type");
            return r;
        }
        /// <summary>
        /// r[0]对应MOD1，r[1]对应MOD2或r[0]对应Mii-，r[1]对应Mii+，无效值为-1
        /// 值域是[0, 1]，因为theta的范围是0到30度
        /// </summary>
        public static double[] GetCos3Theta(double val, double dt, int p, int type)
        {
            // 注意实现的时候，对于半导体管，获得3theta的值的时候，是假设了一次mod1一次mod2的，在调用
            // GetEnergy_Cos3Theta的时候，也要使用对应的假设mod和对应的p'（应该是p + 1或者p - 1）来。返回两个值。
            // type: 0: Air-suspended, 1: SiO2/Si, 2: Quartz, 3: Super-growth, 4: SDS-disp., 5: ssDNA-disp.
            double[] r = new double[2];

            if (type <= 2)
            {
                if (p >= 9)
                    throw new ArgumentOutOfRangeException("higher than S66 not available");
                // the cos3theta got from this function are always reused in GetEnergy, so don't worry if the
                // dt exception is not throwed.
                val += (type == 1) ? 0.04 : ((type == 2) ? 0.1 : 0);
                if (IsMetal(p))
                {
                    r[0] = (param[p, 0] / dt - param[p, 1] / (dt * dt) - val) / param[p, 2] * (dt * dt); // Mii-
                    r[1] = (-(param[p, 0] / dt - param[p, 1] / (dt * dt) - val)) / param[p, 2] * (dt * dt); // Mii+
                }
                else
                {
                    r[0] = (-(param[p, 0] / dt - param[p, 1] / (dt * dt) - val))
                        / param[p, 2] * (dt * dt) * (((p % 3) == (1 % 2)) ? -1 : 1); //mod1
                    r[1] = (-(param[p, 0] / dt - param[p, 1] / (dt * dt) - val))
                        / param[p, 2] * (dt * dt) * (((p % 3) == (2 % 2)) ? -1 : 1); //mod2
                }
            }
            else if (type == 3)
            {
                if (p >= 6)
                    throw new ArgumentOutOfRangeException("p", "higher than M22 not available for Super-Growth");
                double a = 1.074, b = 0.467, c = 0.812;
                double calc(int extmod) =>
                    (val - a * (p + 1) / dt * (1 + b * Math.Log10(c / ((p + 1) / dt))) -
                    ((p > 2) ? 0.059 * (p + 1) / dt : 0)) / betap[p, extmod] * (dt * dt); //extra for larger than M11; warning: p + 1
                r[0] = calc(0); //Mii- or MOD1
                r[1] = calc(1);
            }
            else if (type == 4 || type == 5)
            {
                val += (type == 5) ? 0.02 : 0;
                if (p == 0)
                {
                    r[0] = (val - 1 / (0.1270 + 0.8606 * dt)) / 0.04575 * (dt * dt); // MOD1
                    r[1] = (val - 1 / (0.1270 + 0.8606 * dt)) / (-0.08802) * (dt * dt);
                }
                else if (p == 1)
                {
                    r[0] = (val - 1 / (0.1174 + 0.4644 * dt)) / (-0.1829) * (dt * dt); //MOD1
                    r[1] = (val - 1 / (0.1174 + 0.4644 * dt)) / 0.1705 * (dt * dt);
                }
                else
                    throw new ArgumentOutOfRangeException("p", "only S11 and S22 are available for SDS-disp. or ssDNA disp.");
            }
            else
                throw new ArgumentOutOfRangeException("type", "invalid type");
            r[0] = Math.Round(r[0], 4); // 4位小数
            r[1] = Math.Round(r[1], 4);
            if (r[0] > 1 || r[0] < 0)
                r[0] = -1;
            if (r[1] > 1 || r[1] < 0)
                r[1] = -1;
            return r;
        }
        
        public static double? GetAverage(double splitting, double wRBM, int p_lesser, int type)
        {
            double dt = RBMtoDt(wRBM, p_lesser, type);
            if (IsMetal(p_lesser + 1))
                throw new ArgumentException("p should be the smaller one");
            if (type <= 2)
            {
                if (p_lesser >= 9)
                    throw new ArgumentOutOfRangeException("higher than S66 not available");
                // redshift equalized
                if (IsMetal(p_lesser))
                {
                    double cos3Theta = dt * dt * splitting / 2 * param[p_lesser, 2];
                    if (cos3Theta < 0 || cos3Theta > 1)
                        return null;
                    try
                    {
                        return (GetEnergy_Cos3Theta(dt, cos3Theta, p_lesser, type, -1) + GetEnergy_Cos3Theta(dt, cos3Theta, p_lesser, type, 0)) / 2;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        if (e.ParamName == "dt")
                            return null;
                        else throw e;
                    }
                }
                else
                {
                    int p_larger = p_lesser + 1;
                    double delta(int x) => param[p_larger, x] - param[p_lesser, x];
                    int mod = 1;
                    double cos3Theta = (splitting * dt * dt - delta(0) * dt + delta(1)) /
                        (param[p_larger, 2] * (((p_larger % 3) == (mod % 2)) ? -1 : 1) -
                        param[p_lesser, 2] * (((p_lesser % 3) == (mod % 2)) ? -1 : 1));
                    if (cos3Theta < 0)
                    {
                        mod = 2;
                        cos3Theta = -cos3Theta;
                    }
                    if (cos3Theta > 1)
                        return null;
                    try
                    {
                        return (GetEnergy_Cos3Theta(dt, cos3Theta, p_larger, type, mod) + GetEnergy_Cos3Theta(dt, cos3Theta, p_lesser, type, mod)) / 2;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        if (e.ParamName == "dt")
                            return null;
                        else throw e;
                    }
                }
            }
            else if (type == 3)
            {
                double a = 1.074, b = 0.467, c = 0.812;
                double sgE(int p) => a * (p + 1) / dt * (1 + b * Math.Log10(c / ((p + 1) / dt)))
                            + ((p > 2) ? 0.059 * (p + 1) / dt : 0);
                if (IsMetal(p_lesser))
                {
                    //p related are all equalized
                    double cos3Theta = (splitting) * dt * dt / (betap[p_lesser, 1] - betap[p_lesser, 0]);
                    if (cos3Theta < 0 || cos3Theta > 1)
                        return null;
                    return (GetEnergy_Cos3Theta(dt, cos3Theta, p_lesser, type, -1) + GetEnergy_Cos3Theta(dt, cos3Theta, p_lesser, type, 0)) / 2;
                }
                else
                {
                    int p_larger = p_lesser + 1;
                    int mod = 1;
                    if (p_lesser >= 6)
                        throw new ArgumentOutOfRangeException("p", "higher than M22 not available for Super-Growth");
                    double cos3Theta = (splitting + sgE(p_lesser) - sgE(p_larger)) * dt * dt / (betap[p_larger, mod - 1] - betap[p_lesser, mod - 1]);
                    if (cos3Theta < 0 || cos3Theta > 1)
                    {
                        mod = 2;
                        cos3Theta = (splitting + sgE(p_lesser) - sgE(p_larger)) * dt * dt / (betap[p_larger, mod - 1] - betap[p_lesser, mod - 1]);
                    }
                    if (cos3Theta < 0 || cos3Theta > 1)
                        return null;
                    return (GetEnergy_Cos3Theta(dt, cos3Theta, p_larger, type, mod) + GetEnergy_Cos3Theta(dt, cos3Theta, p_lesser, type, mod)) / 2;
                }
            }
            else if (type == 4 || type == 5)
            {
                if (p_lesser != 0)
                    throw new ArgumentOutOfRangeException("p", "only S11 and S22 are available for SDS-disp. or ssDNA disp.");
                int mod = 1;
                double cos3Theta = (splitting - (1 / (0.1174 + 0.4644 * dt) - 1 / (0.1270 + 0.8606 * dt))) * dt * dt / (-0.1829 - 0.04575);
                if (cos3Theta > 1 || cos3Theta < 0)
                {
                    mod = 2;
                    cos3Theta = (splitting - (1 / (0.1174 + 0.4644 * dt) - 1 / (0.1270 + 0.8606 * dt))) * dt * dt / (0.1705 - -0.08802);
                }
                if (cos3Theta > 1 || cos3Theta < 0)
                    return null;
                return (GetEnergy_Cos3Theta(dt, cos3Theta, 1, type, mod) + GetEnergy_Cos3Theta(dt, cos3Theta, 0, type, mod)) / 2;
            }
            else
                throw new ArgumentOutOfRangeException("type", "invalid type");
        }
        public static List<double[]> GetList(int p, int type)
        {
            List<double[]> li = new List<double[]>();
            if (!IsMetal(p))
            {
                if (IsMetal(p + 1))
                    throw new ArgumentException("p should be the smaller one, e.g. S11 rather than S22");
                for (int n = nMin; n < nMax; n++)
                    for (int m = 0; m <= n; m++)
                    {
                        if (2 * n + m > seriesThreshold)
                            break;
                        try
                        {
                            if (!IsMetal(n, m))
                            {
                                double dl = GetEnergy(Dt(n, m, type), Theta(n, m), p, type, Mod(n, m));
                                double dh = GetEnergy(Dt(n, m, type), Theta(n, m), p + 1, type, Mod(n, m));
                                li.Add(new double[] { n, m, (dh + dl) / 2, (dh - dl) });
                            }
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            if (e.ParamName != "result" && e.ParamName != "dt")
                                throw e;
                        }
                    }
            }
            else
            {
                for (int n = nMin; n < nMax; n++)
                    for (int m = 0; m <= n; m++)
                    {
                        if (2 * n + m > seriesThreshold)
                            break;
                        try
                        {
                            if (IsMetal(n, m))
                            {
                                double dl = GetEnergy(Dt(n, m, type), Theta(n, m), p, type, -1);
                                double dh = GetEnergy(Dt(n, m, type), Theta(n, m), p, type, 0);
                                li.Add(new double[] { n, m, (dh + dl) / 2, (dh - dl) });
                            }
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            if (e.ParamName != "result" && e.ParamName != "dt")
                                throw e;
                        }
                    }
            }
            return li;
        }
        public static Dictionary<int, double[][]> GetRBMArray(int p_lesser, int type)
        {
            var d = new Dictionary<int, double[][]>();
            double cos3ThetaMax = 40;
            for (int rbm = wRBM_min; rbm <= wRBM_max; rbm += 10)
            {
                double dt = RBMtoDt(rbm, p_lesser, type);
                List<double[]> t = new List<double[]>();
                try
                {
                    if (IsMetal(p_lesser))
                    {
                        // fix the bug which leads to large rbm returning to the smalle
                        double plus = GetEnergy_Cos3Theta(dt, cos3ThetaMax, p_lesser, type, 0); //should it be higher?
                        double minus = GetEnergy_Cos3Theta(dt, cos3ThetaMax, p_lesser, type, -1);
                        t.Add(new double[] { (plus + minus) / 2, plus - minus });
                        plus = GetEnergy_Cos3Theta(dt, -cos3ThetaMax, p_lesser, type, 0);
                        minus = GetEnergy_Cos3Theta(dt, -cos3ThetaMax, p_lesser, type, -1);
                        t.Add(new double[] { (plus + minus) / 2, plus - minus });
                        d.Add(rbm, t.ToArray());
                    }
                    else
                    {
                        double plus = GetEnergy_Cos3Theta(dt, cos3ThetaMax, p_lesser + 1, type, 1); //should it be higher?
                        double minus = GetEnergy_Cos3Theta(dt, cos3ThetaMax, p_lesser, type, 1);
                        t.Add(new double[] { (plus + minus) / 2, plus - minus });
                        plus = GetEnergy_Cos3Theta(dt, 0, p_lesser + 1, type, 2);
                        minus = GetEnergy_Cos3Theta(dt, 0, p_lesser, type, 2);
                        t.Add(new double[] { (plus + minus) / 2, plus - minus });
                        plus = GetEnergy_Cos3Theta(dt, cos3ThetaMax, p_lesser + 1, type, 2);
                        minus = GetEnergy_Cos3Theta(dt, cos3ThetaMax, p_lesser, type, 2);
                        t.Add(new double[] { (plus + minus) / 2, plus - minus });
                        d.Add(rbm, t.ToArray());
                    }
                }
                catch (ArgumentOutOfRangeException e)
                {
                    if (e.ParamName == "dt")
                        break; // rbm increases, dt decreases
                    else throw e;
                }
            }
            return d;
        }
    }

    
}