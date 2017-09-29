using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using static NTAssign.Energy;

namespace NTAssign.Models
{

    public class AssignModel
    {
        [Range(0, 5)]
        public int Env { get; set; }
        [Range(0, 11)]
        public int P1 { get; set; }
        public double? Val1 { get; set; }
        [Range(0, 11)]
        public int P2 { get; set; }
        public double? Val2 { get; set; }
        public double? RBM { get; set; }

        public AssignResult GetPlotModel(out PlotModel pm)
        {
            if (Val1 is null && Val2 is null)
            {
                pm = null;
                return AssignResult.error;
            }
            else if (Val1 is null || Val2 is null)
                return (pm = E1R1()).ar;
            else
                return (pm = E2()).ar;
        }
        // sqrt log exceptions
        public PlotModel E1R1()
        {
            int p1 = (Val1 is null) ? P2 : P1, type = Env, p = pArr_p1[p1];
            double val1 = (Val1 ?? Val2) ?? -1, wRBM = RBM ?? -1;
            if (val1 < 0 || wRBM < 0)
                throw new ArgumentException("missing parameter");
            double dt = RBMtoDt(wRBM, p, type);
            double[] cos = GetCos3Theta(val1, dt, p, type);
            string resultString = "";
            if (cos.All(elem => elem == -1))
            {
                resultString += "Invalid input: out of range.";
                return new PlotModel() { ar = AssignResult.error, resultString = resultString };
            }
            int p2, mod2; //mod1
            if (IsMetal(p))
            {
                if (p1 % 4 - 3 != (cos[0] == -1 ? 0 : -1))
                {
                    resultString += @"Invalid input: out of range. You may have mistakenly put " + p1Arr[p1] + " instead of " +
                         p1Arr[p1 + 5 - (p1 % 4) * 2] + ".";
                    return new PlotModel() { ar = AssignResult.error, resultString = resultString };
                }
                p2 = p;
                mod2 = cos[0] == -1 ? -1 : 0;
            }
            else
            {
                p2 = IsMetal(p + 1) ? p - 1 : p + 1;
                mod2 = cos[0] == -1 ? 2 : 1; // == mod1
            }
            double val2 = GetEnergy_Cos3Theta(dt, cos[0] == -1 ? cos[1] : cos[0], p2, type, mod2);
            if ((IsMetal(p) && (mod2 == -1)) || (!IsMetal(p) && (p > p2)))
            {
                Swap(ref p, ref p2);
                Swap(ref val1, ref val2);
            }
            double x = (val1 + val2) / 2, y = val2 - val1;
            return Assign(new PlotModel()
            {
                bluePoint = null,
                point = new double[] { x, y },
                p_lesser = p,
                type = type,
                pointType = "green",
                p1_lesser = p1 % 2 == 0 ? p1 : p1 - 1,
                resultString = resultString
            }, mod2);

        }


        public PlotModel E2()
        {
            string resultString = "";
            int p1 = pArr_p1[P1], p2 = pArr_p1[P2];
            int p1_ = P1, p2_ = P2, type = Env;
            double val1 = Val1 ?? -1, val2 = Val2 ?? -1;
            if (p1_ > p2_)
            {
                Swap(ref p1, ref p2);
                Swap(ref val1, ref val2);
                Swap(ref p1_, ref p2_);
            }
            if (val1 < 0 || val2 < 0)
                throw new ArgumentException("missing parameter");
            if (IsMetal(p2) != IsMetal(p1))
                throw new UnauthorizedAccessException("invalid form submission");


            if (p2_ - p1_ == 1)
            {
                double[] bluePoint = null;
                if (RBM.HasValue && GetAverage(val2 - val1, RBM.Value, p1, type) is double average)
                    bluePoint = new double[] { average, val2 - val1 };
                else if (RBM.HasValue)
                    resultString += "Invald input: out of range. Please check your input RBM value. Only transition energies are processed. " +
                        "<br/ >";
                return Assign(new PlotModel()
                {
                    point = new double[] { (val1 + val2) / 2, val2 - val1 },
                    p_lesser = p1,
                    type = type,
                    pointType = "red",
                    bluePoint = bluePoint,
                    p1_lesser = p1_,
                    resultString = resultString,
                });

            }
            else throw new NotImplementedException();
            #region comment
            /*else
            {
                if (type <= 2)
                {
                    val1 += (type == 1) ? 0.04 : ((type == 2) ? 0.1 : 0);
                    val2 += (type == 1) ? 0.04 : ((type == 2) ? 0.1 : 0);
                    if (IsMetal(p1))
                    {
                        int mod1 = p1_ % 4 - 3, mod2 = p2_ % 4 - 3;
                        double gamma1 = param[p1, 2] * (mod1 * 2 + 1);
                        double gamma2 = param[p2, 2] * (mod2 * 2 + 1);
                        double mA = val1 / gamma1 - val2 / gamma2;
                        double mB = -(param[p1, 0] / gamma1 - param[p2, 0] / gamma2);
                        double mC = param[p1, 1] / gamma1 - param[p2, 1] / gamma2;
                        double cos3Theta = -1, dt = -1;
                        // solve the quadratic eqn.
                        try
                        {
                            dt = (-mB + Math.Sqrt(mB * mB - 4 * mA * mC)) / (2 * mA);
                            cos3Theta = GetCos3Theta(val1, dt, p1, type)[mod1 + 1];
                        }
                        catch (Exception)
                        {
                            // nothing
                        }
                        if (cos3Theta == -1)
                        {
                            resultString += @"Yout may have mistaken the \(M_{ii}^+\) and \(M_{ii}^-\), for the " +
                                @"\(\cos(3\theta)\) solved from the input value is invalid.";
                            return AssignResult.error;
                        }
                        // NOW we have the valid data of cos3theta and dt.
                        double val1_pair = GetEnergy_Cos3Theta(dt, cos3Theta, p1, type, -mod1 - 1);
                        double val2_pair = GetEnergy_Cos3Theta(dt, cos3Theta, p2, type, -mod2 - 1);
                        AssignResult ar1 = Assign(new double[] { (val1 + val1_pair) / 2, (val2 - val1) * (mod1 == 0 ? -1 : 1) }, p1, type, out List<double[]> result1, out List<double[]> all1);
                        AssignResult ar2 = Assign(new double[] { (val1 + val2_pair) / 2, (val2 - val1) * (mod1 == 0 ? -1 : 1) }, p2, type, out List<double[]> result2, out List<double[]> all2);

                    }
                    else
                    {
                        void calc(out double dt_, out double cos3Theta_, int mod_)
                        {
                            double gamma1 = param[p1, 2] * (mod_ * 2 - 3);
                            double gamma2 = param[p2, 2] * (mod_ * 2 - 3);
                            double mA = val1 / gamma1 - val2 / gamma2;
                            double mB = -(param[p1, 0] / gamma1 - param[p2, 0] / gamma2);
                            double mC = param[p1, 1] / gamma1 - param[p2, 1] / gamma2;
                            cos3Theta_ = -1;
                            dt_ = -1;
                            // solve the quadratic eqn.
                            try
                            {
                                dt_ = (-mB + Math.Sqrt(mB * mB - 4 * mA * mC)) / (2 * mA);
                                cos3Theta_ = GetCos3Theta(val1, dt_, p1, type)[mod_ + 1];
                            }
                            catch (Exception)
                            {
                                // nothing
                            }
                        }
                        int mod = 1;
                        calc(out double dt, out double cos3Theta, mod);
                        if (cos3Theta == -1)
                        {
                            resultString += @"Yout may have mistaken the \(M_{ii}^+\) and \(M_{ii}^-\), for the " +
                                @"\(\cos(3\theta)\) solved from the input value is invalid.";
                            return AssignResult.error;
                        }
                        // NOW we have the valid data of cos3theta and dt.
                        double val1_pair = GetEnergy_Cos3Theta(dt, cos3Theta, p1, type, -mod1 - 1);
                        double val2_pair = GetEnergy_Cos3Theta(dt, cos3Theta, p2, type, -mod2 - 1);
                        AssignResult ar1 = Assign(new double[] { (val1 + val1_pair) / 2, (val2 - val1) * (mod1 == 0 ? -1 : 1) }, p1, type, out List<double[]> result1, out List<double[]> all1);
                        AssignResult ar2 = Assign(new double[] { (val1 + val2_pair) / 2, (val2 - val1) * (mod1 == 0 ? -1 : 1) }, p2, type, out List<double[]> result2, out List<double[]> all2);

                    }
                }
                else if (type == 3)
                {
                    double a = 1.074, b = 0.467, c = 0.812;
                    double calc(int extmod) =>
                        (val - a * (p + 1) / dt * (1 + b * Math.Log10(c / ((p + 1) * dt))) -
                        ((p > 2) ? 0.059 * (p + 1) / dt : 0)) / betap[p, extmod] * (dt * dt); //extra for larger than M11; warning: p + 1
                    r[0] = calc(0); //Mii- or MOD1
                    r[1] = calc(1);
                }
            }*/
            #endregion
        }

        public PlotModel Assign(PlotModel pm, int mod = -1)
        {
            // x: average y: splitting
            double Dist(double x1, double y1, double x2, double y2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            double deltaX = 0.6, maxY = IsMetal(pm.p_lesser) ? 0.6 : pm.point[1] + 0.6, minY = IsMetal(pm.p_lesser) ? -0.1 : pm.point[1] - 0.6;
            pm.all = GetList(pm.p_lesser, pm.type)
                    .Where(e => (e[2] >= pm.point[0] - deltaX && e[2] <= pm.point[0] + deltaX && e[3] <= maxY && e[3] >= minY))
                    .ToList();


            double? dxmin_p = null, dxmax_p = null, dymin_p = null, dymax_p = null;
            if (pm.pointType == "red")
            {
                double? dxmin = null, dxmax = null;
                if (pm.bluePoint != null)
                {
                    if (pm.bluePoint[0] - pm.point[0] < 0.02)
                    {
                        dxmin = -0.008;
                        dxmax = 0.008;
                    }
                    else
                    {
                        dxmin = -0.030;
                        dxmax = -0.005; // TODO: NOTICE HERE
                    }
                }
                pm.result = pm.all
                    .Where(e => (
                    (mod == -1 || IsMetal(pm.p_lesser) || mod == Mod((int)e[0], (int)e[1])) && 
                    pm.point[0] - e[2] >= (dxmin ?? -0.025) && pm.point[0] - e[2] <= (dxmax ?? 0.008) && 
                    pm.point[1] - e[3] <= 0.015 && pm.point[1] - e[3] >= -0.015
                    ))
                    .ToList();
                if (pm.result.Count > 0)
                {
                    pm.ar = AssignResult.accurate;
                    pm.resultString += "The assignment result is:<br /><font style=\"font-size: 28px;\">";
                    pm.result = pm.result.OrderBy(e => Dist(e[2], e[3], pm.point[0], pm.point[1])).ToList();
                    for (int i = 0; i < pm.result.Count; i++)
                        pm.resultString += "<b>(" + (int)pm.result[i][0] + "," + (int)pm.result[i][1] + ")</b>" + 
                            (i == pm.result.Count ? ", " : "");
                    pm.resultString += "</font>";
                    return pm;
                }
            }
            else if (pm.pointType == "green")
            {
                dymin_p = -0.070;
                dymax_p = 0.070;
                dxmax_p = 0.040;
                dxmin_p = -0.040;
            }
            pm.result = pm.all
                .Where(e => (
                (mod == -1 || IsMetal(pm.p_lesser) || mod == Mod((int)e[0], (int)e[1])) && 
                pm.point[0] - e[2] >= (dxmin_p ?? -0.040) && pm.point[0] - e[2] <= (dxmax_p ?? 0.0126) && 
                pm.point[1] - e[3] <= (dymax_p ?? 0.030) && pm.point[1] - e[3] >= (dymin_p ?? -0.030)
                ))
                .ToList();
            if (pm.result.Count > 0)
            {
                pm.ar = AssignResult.possible;
                pm.resultString += "The likely assignments include:<br/><font style=\"font-size: 28px;\">";
                pm.result = pm.result.OrderBy(e => Dist(e[2], e[3], pm.point[0], pm.point[1])).ToList();
                for (int i = 0; i < pm.result.Count; i++)
                    pm.resultString += "<b>(" + (int)pm.result[i][0] + "," + (int)pm.result[i][1] + ")</b>" +
                        (i != pm.result.Count - 1 ? ", " : "");
                pm.resultString += "</font>";
                return pm;
            }
            dymin_p = -0.070;
            dymax_p = 0.070;
            dxmax_p = 0.040;
            dxmin_p = -0.040;
            pm.result = pm.all
                .Where(e => (
                (mod == -1 || IsMetal(pm.p_lesser) || mod == Mod((int)e[0], (int)e[1])) &&
                pm.point[0] - e[2] >= (dxmin_p ?? -0.040) && pm.point[0] - e[2] <= (dxmax_p ?? 0.010) &&
                pm.point[1] - e[3] <= (dymax_p ?? 0.020) && pm.point[1] - e[3] >= (dymin_p ?? -0.020)
                ))
                .ToList();
            // use the green 
            if (pm.result.Count == 0)
            {
                pm.ar = AssignResult.error;
                pm.resultString = "Invalid input: out of range. Please check your input.";
                return pm;
            }
            pm.resultString += "No match. The possible results include:<br /><font style=\"font-size: 28px;\">";
            pm.result = pm.result.OrderBy(e => Dist(e[2], e[3], pm.point[0], pm.point[1])).ToList();
            for (int i = 0; i < pm.result.Count; i++)
                pm.resultString += "<b>(" + (int)pm.result[i][0] + "," + (int)pm.result[i][1] + ")</b>" +
                    (i != pm.result.Count - 1 ? ", " : "");
            pm.resultString += "</font>";
            pm.ar = AssignResult.impossible;
            return pm;
        }


        public Tuple<List<string[]>, List<int[]>, string> GetParams()
        {
            string ToMath(string s) => @"\(" + s + @"\)";
            var arr = new List<string[]>();
            var colspan = new List<int[]>();
            var reflist = new List<int>();
            var references = new string[] {
                "//just placeholder",
                "Liu, K. H. <i>et al.</i> An atlas of carbon nanotube optical transitions. <i>Nat. Nanotech.</i> <b>7</b>, 325-329 (2012).",
                "Zhang, D. Q. <i>et al.</i> (n,m) Assignments of Metallic Single-Walled Carbon Nanotubes by Raman Spectroscopy: The Importance of Electronic Raman Scattering. <i>ACS Nano</i> <b>10</b>, 10789-10797 (2016).",
                "Meyer, J. C. <i>et al.</i> Raman modes of index-identified freestanding single-walled carbon nanotubes. <i>Phys. Rev. Lett.</i> <b>95</b>, 217401 (2005).",
                "Liu, K. H. <i>et al.</i> High-throughput optical imaging and spectroscopy of individual carbon nanotubes in devices. <i>Nat. Nanotech.</i> <b>8</b>, 917-922 (2013).",
                "Zhang, D. Q. <i>et al.</i> (n,m) Assignments and quantification for single-walled carbon nanotubes on SiO2/Si substrates by resonant Raman spectroscopy. <i>Nanoscale</i> <b>7</b>, 10719-10727 (2015).",
                "Soares, J. S. <i>et al.</i> The Kataura plot for single wall carbon nanotubes on top of crystalline quartz. <i>Phys. Stat. Soli. B</i> <b>247</b>, 2835-2837 (2010).",
                "Araujo, P. T. <i>et al.</i> Third and fourth optical transitions in semiconducting carbon nanotubes. <i>Phys. Rev. Lett.</i> <b>98</b>, 067401 (2007).",
                "Araujo, P. T. <i>et al.</i> Resonance Raman spectroscopy of the radial breathing modes in carbon nanotubes. <i>Phys. E</i> <b>42</b>, 1251-1261 (2010).",
                "Araujo, P. T. <i>et al.</i> Nature of the constant factor in the relation between radial breathing mode frequency and tube diameter for single-wall carbon nanotubes. <i>Phys. Rev. B</i> <b>77</b>, 241403 (2008).",
                "Bachilo, S. M. <i>et al.</i> Structure-assigned optical spectra of single-walled carbon nanotubes. <i>Science</i> <b>298</b>, 2361-2366 (2002).",
                "Tu, X. M. <i>et al.</i> DNA sequence motifs for structure-specific recognition and separation of carbon nanotubes. <i>Nature</i> <b>460</b>, 250-253 (2009).",
                "Fantini, C. <i>et al.</i> Characterization of DNA-wrapped carbon nanotubes by resonance Raman and optical absorption spectroscopies. <i>Chem. Phys. Lett.</i> <b>439</b>, 138-142 (2007)."
            };
            var doi = new string[] {
                "//just placeholder",
                "10.1038/nnano.2012.52",
                "10.1021/acsnano.6b04453",
                "10.1103/PhysRevLett.95.217401",
                "10.1038/nnano.2013.227",
                "10.1039/C5NR01076D",
                "10.1002/pssb.201000239",
                "10.1103/PhysRevLett.98.067401",
                "10.1016/j.physe.2010.01.015",
                "10.1103/PhysRevB.77.241403",
                "10.1126/science.1078727",
                "10.1038/nature08116",
                "10.1016/j.cplett.2007.03.085"
            };
            switch (Env)
            {
                case 0:
                case 1:
                case 2:
                    colspan.Add(new int[] { 5 });
                    arr.Add(new string[] { @"$$\begin{align*}E_{ii}(p)=\frac{\alpha(p)}{d_t}-\frac{\beta(p)}{d_t^2}+\frac{\gamma(p)}{d_t^2}\cos(3\theta)\quad[1]\end{align*}$$" });
                    reflist.Add(2);
                    break;
                case 3:
                    colspan.Add(new int[] { 4 });
                    arr.Add(new string[] { @"$$\begin{align*}E_{ii}(p)=a\frac{p}{d_t}(1+b\log\frac{c}{p/d_t})+\frac{\beta_p}{d_t^2}\cos(3\theta)+\Delta(p)\end{align*}$$" +
                        @"\(a=1.074\ \mathrm{eV\ nm},b=0.467\ \mathrm{nm^{-1}},c=0.812\ \mathrm{nm^{-1}}\)" + "<br />" + @"Only when \(p\lt3\), \(\Delta(p)=0.059p/d_t\ \mathrm{eV}\quad[1]\)"});
                    reflist.Add(8);
                    break;
                case 4:
                case 5:
                    colspan.Add(new int[] { 4 });
                    arr.Add(new string[] { @"$$\begin{align*}&E_{ii}(p)=\frac{1}{\alpha+\beta d_t}+\frac{\gamma(p)}{d_t^2}\cos(3\theta)\end{align*}$$" +
                        @"\(\alpha=0.1270\ \mathrm{eV^{-1}},\beta=0.8606\ \mathrm{nm^{-1}\ eV^{-1}}\quad[1]\)" });
                    reflist.Add(10);
                    break;
            }
            switch (Env)
            {
                case 1:
                    arr[0][0] += @"All \(E_{ii}\) are \(40\ \mathrm{meV}\) redshifted from " + envArr[1] + @" (below).\(\quad[2]\)";
                    reflist.Add(4);
                    break;
                case 2:
                    arr[0][0] += @"All \(E_{ii}\) are \(100\ \mathrm{meV}\) redshifted from " + envArr[1] + @" (below).\(\quad[2]\)";
                    reflist.Add(6);
                    break;
                case 5:
                    arr[0][0] += @"<br />All \(E_{ii}\) are \(20\ \mathrm{meV}\) redshifted from " + envArr[4] + @" (below).\(\quad[2]\)";
                    reflist.Add(11);
                    break;
            }
            string s_ = null;
            switch (Env)
            {
                case 1:
                case 2:
                    s_ = "[2][3]";
                    goto case 0;
                case 0:
                    colspan.Add(new int[] { 1, 1, 1, 1, 1 });
                    arr.Add(new string[] { @"\(p\)", @"\(E_{ii}\)",
                        @"\(\alpha(p)/(\mathrm{eV\ nm})\)", @"\(\beta(p)/(\mathrm{eV\ nm^2})\)",
                        @"\(\gamma(p)/(\mathrm{eV\ nm^2})^a\)" });
                    for (int i = 0; i < 9; i++)
                    {
                        colspan.Add(new int[] { 1, 1, 1, 1, 1 });
                        arr.Add(new string[] {ToMath((i + 1).ToString()), pArr[i],
                            ToMath(param[i, 0].ToString("f3")), ToMath(param[i, 1].ToString("f3")),
                            ToMath(@"\pm" + param[i, 2].ToString("f3")) });
                    }
                    colspan.Add(new int[] { 5 });
                    arr.Add(new string[] { @"\(^a\)For M-SWNTs, \(\gamma(p)\) is negative for \(M_{ii}^-\) and positive for \(M_{ii}^+\).<br />For MOD1 S-SWNTs, \(\gamma(p)\) is negative when \(i\) is even and positive when \(i\) is odd.<br />For MOD2 S-SWNTs, \(\gamma(p)\) is negative when \(i\) is odd and positive when \(i\) is even.\(\quad" + (s_ ?? "[1][2]") + @"\)" });
                    reflist.Add(1);
                    break;
                case 3:
                    colspan.Add(new int[] { 1, 1, 1, 1 });
                    arr.Add(new string[] { @"\(p\)", @"\(E_{ii}\)",
                        @"\(\beta_p/(\mathrm{eV\ nm^2})\ (M_{ii}^-\ \mathrm{or\ MOD1})\)",
                        @"\(\beta_p/(\mathrm{eV\ nm^2})\ (M_{ii}^+\ \mathrm{or\ MOD2})\)" });
                    for (int i = 0; i < 6; i++)
                    {
                        colspan.Add(new int[] { 1, 1, 1, 1 });
                        arr.Add(new string[] {ToMath((i + 1).ToString()), pArr[i],
                            ToMath(betap[i, 0].ToString("f2")), ToMath(betap[i, 1].ToString("f2"))});
                    }
                    break;
                case 4:
                case 5:
                    colspan.Add(new int[] { 1, 1, 1, 1 });
                    arr.Add(new string[] { @"\(p\)", @"\(E_{ii}\)",
                        @"\(\gamma_p/(\mathrm{eV\ nm^2})\ (\mathrm{MOD1})\)",
                        @"\(\gamma_p/(\mathrm{eV\ nm^2})\ (\mathrm{MOD2})\)" });
                    double[,] array = new double[,] { { 0.04575, -0.08802 }, { -0.1829, 0.1705 } };
                    for (int i = 0; i < 2; i++)
                    {
                        colspan.Add(new int[] { 1, 1, 1, 1 });
                        arr.Add(new string[] {ToMath((i + 1).ToString()), pArr[i],
                            ToMath(array[i, 0].ToString("f4")), ToMath(betap[i, 1].ToString("f4"))});
                    }
                    break;
            }
            colspan.Add(colspan[0]);
            arr.Add(new string[] { @"$$\begin{align*}\omega_\mathrm{RBM}=\frac{A}{d_t}+B\end{align*}$$" });
            switch (Env)
            {
                case 0:
                    arr[arr.Count - 1][0] += @"$$\begin{align*}&p=1,2:A=204\ \mathrm{nm\ cm^{-1}},B=27\ \mathrm{cm^{-1}}\quad[3]\\&p=3:A=200\ \mathrm{nm\ cm^{-1}},B=26\ \mathrm{cm^{-1}}\quad[1]\\&\mathrm{others}:A=228\ \mathrm{nm\ cm^{-1}},B=0\ \mathrm{cm^{-1}}\quad[2]\end{align*}$$";
                    reflist.Add(3);
                    break;
                case 1:
                    arr[arr.Count - 1][0] += @"\(A=235.9\ \mathrm{nm\ cm^{-1}},B=5.5\ \mathrm{cm^{-1}}\quad[4]\)";
                    reflist.Add(5);
                    break;
                case 2:
                    arr[arr.Count - 1][0] += @"\(A=217.8\ \mathrm{nm\ cm^{-1}},B=15.7\ \mathrm{cm^{-1}}\quad[4]\)";
                    reflist.Add(7);
                    break;
                case 3:
                    arr[arr.Count - 1][0] += @"\(A=227.0\ \mathrm{nm\ cm^{-1}},B=0.3\ \mathrm{cm^{-1}}\quad[2]\)";
                    reflist.Add(9);
                    break;
                case 4:
                    arr[arr.Count - 1][0] += @"\(A=223.5\ \mathrm{nm\ cm^{-1}},B=12.5\ \mathrm{cm^{-1}}\quad[1]\)";
                    break;
                case 5:
                    arr[arr.Count - 1][0] += @"\(A=218\ \mathrm{nm\ cm^{-1}},B=18.3\ \mathrm{cm^{-1}}\quad[3]\)";
                    reflist.Add(12);
                    break;

            }
            string refstr = "";
            for (int i = 0; i < reflist.Count; i++)
                refstr += "<a target='_blank' href='http://doi.org/" + doi[reflist[i]] + "'>[" + (i + 1) + "] " + references[reflist[i]] + (i != reflist.Count - 1 ? "<br /><a/>" : "");
            return new Tuple<List<string[]>, List<int[]>, string>(arr, colspan, refstr);
        }

    }
}