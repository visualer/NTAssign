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
        [Range(0, 60)]
        public int NCalc { get; set; }
        [Range(0, 60)]
        public int MCalc { get; set; }

        public double Uncertainty;

        public Tuple<List<int>, List<double>> Calculator()
        {
            if (NCalc < MCalc)
            {
                int t = NCalc;
                NCalc = MCalc;
                MCalc = t;
            }

            if (NCalc <= 6 || MCalc < 0)
                return null;
            List<int> li1 = new List<int>();
            List<double> li2 = new List<double>();
            for (int i = 0; i < 12; i++)
                try
                {
                    if (IsMetal(p1ToP[i]) != IsMetal(NCalc, MCalc))
                        continue;
                    var val = GetEnergy(Dt(NCalc, MCalc, Env), Theta(NCalc, MCalc),
                        p1ToP[i], Env, IsMetal(NCalc, MCalc) ? i % 2 - 1 : Mod(NCalc, MCalc));
                    li1.Add(i);
                    li2.Add(val);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // pass;
                }
            
            return new Tuple<List<int>, List<double>>(li1, li2);
        }

        public AssignResult GetPlotModel(out PlotModel[] pm)
        {
            int DecimalDigits(string d) => d.Length - (d.IndexOf(".") == -1 ? d.Length : d.IndexOf(".") - 1);
            
            if (Val1 is null && Val2 is null)
            {
                pm = null;
                throw new UnauthorizedAccessException();
            }
            else if (Val1 is null || Val2 is null)
            {
                Uncertainty = 2.0 / Math.Pow(10, DecimalDigits((Val1 ?? Val2.Value).ToString()));
                return (pm = new PlotModel[] { E1R1() })[0].ar;
            }
            else
            {
                Uncertainty = 2.0 / Math.Pow(10, 
                    Math.Min(DecimalDigits(Val1.Value.ToString()), DecimalDigits(Val2.Value.ToString())));
                return (pm = E2())[0].ar;
            }
        }
        // sqrt log exceptions
        public PlotModel E1R1()
        {
            int p1 = (Val1 is null) ? P2 : P1, type = Env, p = p1ToP[p1];
            double val1 = (Val1 ?? Val2) ?? -1, wRBM = RBM ?? -1;
            if (val1 < 0 || wRBM < 0)
                throw new ArgumentException("missing parameter");
            double dt = RBMtoDt(wRBM, p, type);
            double[] cos = GetCos3Theta(val1, dt, p, type);
            string resultString = "";
            PlotModel ProcErr() => Assign(new PlotModel()
            {
                bluePoint = null,
                point = new double[] { val1, 0.23 },
                p_lesser = pToLesser[p],
                type = type,
                pointType = "none",
                p1_lesser = P1ToLesser(p1),
                resultString = resultString
            });

            if (cos.All(elem => elem == -1))
            {
                resultString += "Invalid input: out of range.";
                return ProcErr();
            }
            int p_2, mod2; //mod1
            if (IsMetal(p))
            {
                if (p1 % 4 - 3 != (cos[0] == -1 ? 0 : -1))
                {
                    resultString += @"Invalid input: out of range. You may have mistakenly put " + p1Arr[p1] + " instead of " +
                         p1Arr[p1 + 5 - (p1 % 4) * 2] + ".";
                    return ProcErr();
                }
                p_2 = p;
                mod2 = cos[0] == -1 ? -1 : 0;
            }
            else
            {
                p_2 = IsMetal(p + 1) ? p - 1 : p + 1;
                mod2 = cos[0] == -1 ? 2 : 1; // == mod1
            }
            double val2;
            try
            {
                val2 = GetEnergy_Cos3Theta(dt, cos[0] == -1 ? cos[1] : cos[0], p_2, type, mod2);
            }
            catch (ArgumentOutOfRangeException e)
            {
                if (e.ParamName == "dt")
                {
                    resultString += "Invalid input: out of range, diameter too small.";
                    return ProcErr();
                }
                else throw;
            }
            if ((IsMetal(p) && (mod2 == -1)) || (!IsMetal(p) && (p > p_2)))
            {
                Swap(ref p, ref p_2);
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
                p1_lesser = P1ToLesser(p1),
                resultString = resultString
            }, mod2);

        }


        public PlotModel[] E2()
        {
            string resultString = "";
            int p1 = p1ToP[P1], p2 = p1ToP[P2];
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
                return new PlotModel[] { Assign(new PlotModel()
                {
                    point = new double[] { (val1 + val2) / 2, val2 - val1 },
                    p_lesser = p1,
                    type = type,
                    pointType = "red",
                    bluePoint = bluePoint,
                    p1_lesser = p1_,
                    resultString = resultString

                }) };

            }
            else
            {
                throw new NotImplementedException();
                /*AssignModel as1 = new AssignModel() { Env = Env, P1 = P1, P2 = -1, RBM = RBM, Val1 = Val1, Val2 = Val2 };
                AssignModel as2 = new AssignModel() { Env = Env, P1 = -1, P2 = P2, RBM = RBM, Val1 = Val1, Val2 = Val2 };
                PlotModel pm1 = as1.E1R1();
                PlotModel pm2 = as2.E1R1();*/
            }

        }

        public PlotModel Assign(PlotModel pm, int mod = -1)
        {
            // x: average y: splitting
            double dxmin_p = -1, dxmax_p = -1, dymin_p = -1, dymax_p = -1;
            double Dist(double x1, double y1, double x2, double y2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            double Dist_(double[] e) => Dist(e[2], e[3], pm.point[0], pm.point[1]);
            double deltaX = 0.6, maxY = IsMetal(pm.p_lesser) ? 0.6 : pm.point[1] + 0.6, minY = IsMetal(pm.p_lesser) ? -0.1 : pm.point[1] - 0.6;
            void SetBounds(double dxmin_, double dxmax_, double dymin_, double dymax_)
            {
                dxmin_p = dxmin_;
                dxmax_p = dxmax_;
                dymin_p = dymin_;
                dymax_p = dymax_;
            }
            
            if (pm.pointType == "none")
            {
                pm.ar = AssignResult.error;
                pm.result = new List<double[]>();
                return pm;
            }

            if (Uncertainty > 0.2)
            {
                pm.resultString = "Input uncertainty too large. Please give more significant figures.";
                pm.ar = AssignResult.error;
                pm.result = new List<double[]>();
                return pm;
            }

            pm.all = GetList(pm.p_lesser, pm.type)
                    .Where(e => (e[2] >= pm.point[0] - deltaX && e[2] <= pm.point[0] + deltaX &&
                    e[3] <= maxY && e[3] >= minY))
                    .ToList();

            var query = pm.all
                .Where(e => (
                (mod == -1 || IsMetal(pm.p_lesser) || mod == Mod((int)e[0], (int)e[1])) &&
                pm.point[0] - e[2] >= dxmin_p && pm.point[0] - e[2] <= dxmax_p &&
                pm.point[1] - e[3] >= dymin_p && pm.point[1] - e[3] <= dymax_p
                ));

            SetBounds(-Uncertainty, Uncertainty, -Uncertainty, Uncertainty);
            var uc = query.ToList(); // query once to get uncertainty range

            void ProcessOutput()
            {
                pm.result = query.OrderBy(Dist_).ToList();
                for (int i = 0; i < pm.result.Count; i++)
                    pm.resultString += "<b>(" + (int)pm.result[i][0] + "," + (int)pm.result[i][1] + ")</b>" +
                        (i != pm.result.Count - 1 ? ", " : "");
                pm.resultString += "</font>";
            }

            if (pm.pointType == "red")
            {
                if (pm.bluePoint != null)
                {
                    if (pm.bluePoint[0] - pm.point[0] < 0.02)
                        SetBounds(-0.008, 0.008, -0.015, 0.015);
                    else
                        SetBounds(-0.030, -0.005, -0.015, 0.015); // don't change at this moment
                }
                else
                    SetBounds(-0.020, 0.008, -0.015, 0.015);

                if (query.Count() == 1 && uc.Count <= 1)
                {
                    pm.ar = AssignResult.accurate;
                    pm.resultString += "The assignment result is:<br /><font style=\"font-size: 28px;\">";
                    ProcessOutput();
                    return pm;
                }
                SetBounds(-0.040, 0.0126, -0.030, 0.030);
            }
            else
                SetBounds(-0.040, 0.040, -0.070, 0.070);

            query.Union(uc); // will this cause query to become static? TODO: needs test

            if (query.Count() > 0)
            {
                pm.ar = AssignResult.possible;
                pm.resultString += "The likely assignments include:<br /><font style=\"font-size: 28px;\">";
                ProcessOutput();
                return pm;
            }

            // use the green criteria and query again for no match.
            // and it's easy to see that green point, if not returned in the previous step,
            // will not give results in this step.

            SetBounds(-0.040, 0.040, -0.070, 0.070);
            var tmp = pm.all.OrderBy(Dist_).ToList();
            if (Dist_(tmp[0]) / Dist_(tmp[1]) <= 0.5 && query.Count() != 0) // 50% criteria
            {
                pm.ar = AssignResult.impossible;
                query = new List<double[]> { tmp[0] };
                pm.resultString += "No match. The most possible assignment result is:<br /><font style=\"font-size: 28px;\">";
                ProcessOutput();
                return pm;
            }
            
            pm.ar = AssignResult.error;
            pm.resultString = "Invalid input: out of range. Please check your input.";
            pm.result = new List<double[]>();
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
            bool hasDelta = false;
            switch (Env)
            {
                case 1:
                case 2:
                    hasDelta = true;
                    goto case 0;
                case 0:
                    colspan.Add(new int[] { 5 });
                    arr.Add(new string[] { @"$$\begin{align*}E_{ii}(p)=\frac{\alpha(p)}{d_t}-\frac{\beta(p)}{d_t^2}+\frac{\gamma(p)}{d_t^2}\cos(3\theta)" + (hasDelta ? @"-\Delta" : @"\quad[1]") + @"\end{align*}$$" });
                    reflist.Add(2);
                    break;
                case 3:
                    colspan.Add(new int[] { 4 });
                    arr.Add(new string[] { @"$$\begin{align*}E_{ii}(p)=a\frac{p}{d_t}(1+b\log\frac{c}{p/d_t})+\frac{\beta_p}{d_t^2}\cos(3\theta)+\Delta(p)\end{align*}$$" +
                        @"\(a=1.074\ \mathrm{eV\ nm},b=0.467\ \mathrm{nm^{-1}},c=0.812\ \mathrm{nm^{-1}}\)" + "<br />" + @"\(p\lt3:\ \Delta(p)=0.059p/d_t\ \mathrm{eV}\)" + "<br />" +
                        @"\(p\ge3:\ \Delta(p)=0\quad[1]\)"});
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
                    arr[0][0] += @"\(\Delta=40\ \mathrm{meV}\quad[1][2]\)";
                    reflist.Add(4);
                    break;
                case 2:
                    arr[0][0] += @"\(\Delta=100\ \mathrm{meV}\quad[1][2]\)";
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
                    arr[arr.Count - 1][0] += @"$$\begin{align*}& p=1,2:A=204\ \mathrm{nm\ cm^{-1}},B=27\ \mathrm{cm^{-1}}\quad[3]\\& p=3:A=200\ \mathrm{nm\ cm^{-1}},B=26\ \mathrm{cm^{-1}}\quad[1]\\& \mathrm{others}:A=228\ \mathrm{nm\ cm^{-1}},B=0\ \mathrm{cm^{-1}}\quad[2]\end{align*}$$";
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
                refstr += "<a target='_blank' href='http://doi.org/" + doi[reflist[i]] + "'>[" + (i + 1) + "] " + references[reflist[i]] + (i != reflist.Count - 1 ? "<br /></a>" : "");
            return new Tuple<List<string[]>, List<int[]>, string>(arr, colspan, refstr);
        }

    }
}