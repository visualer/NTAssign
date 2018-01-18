using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static NTAssign.Energy;

namespace NTAssign.Models
{
    public class PlotModel
    {
        public double[] point;
        public List<double[]> all;
        public List<double[]> result;
        public int p_lesser, type, p1_lesser; //p_larger = p_lesser + 1, p1 is for plotting
        public string pointType;
        public double[] bluePoint;
        public string resultString;
        public AssignResult ar;

        public string BluePoint()
        {
            if (bluePoint is null)
                return null;
            else
                return "[" + bluePoint[0].ToString("f4") + "," + bluePoint[1].ToString("f4") + "]";
        }
        public string Point()
        {
            return "[" + point[0].ToString("f4") + "," + point[1].ToString("f4") + "]";
        }
        public Tuple<string, string> All()
        {
            var q = from p in all
                    group p by 2 * p[0] + p[1] into g
                    orderby g.Key
                    select g.OrderBy(elem => elem[0]);
            // note that average energy may not increase monotonously as n in (n,m) increases.
            // thus elem => elem[2] is wrong.
            // test: S11 = 1.420, S22 = 2.134 as (6,4), see branch 2n + m = 16
            List<string> ll = new List<string>();
            List<string> ll_label = new List<string>();
            foreach (var i in q)
            {
                List<string> l = new List<string>();
                List<string> l_label = new List<string>();
                foreach (var j in i)
                {
                    l.Add("[" + j[2].ToString("f4") + "," + j[3].ToString("f4") + "]");
                    l_label.Add("[" + (int)j[0] + "," + (int)j[1] + "]");
                }
                ll.Add("[" + string.Join(",", l) + "]");
                ll_label.Add("[" + string.Join(",", l_label) + "]");
            }
            return new Tuple<string, string>("[" + string.Join(",", ll) + "]", "[" + string.Join(",", ll_label) + "]");
        }
        public Tuple<string, string> Result()
        {
            List<string> l = new List<string>();
            List<string> l_label = new List<string>();
            foreach (var j in result)
            {
                l.Add("[" + j[2].ToString("f4") + "," + j[3].ToString("f4") + "]");
                l_label.Add("[" + (int)j[0] + "," + (int)j[1] + "]");
            }
            return new Tuple<string, string>("[" + string.Join(",", l) + "]", "[" + string.Join(",", l_label) + "]");
        }
        public Tuple<string, string, string> RBM()
        {
            double ymax = Energy.IsMetal(p_lesser) ? 0.51 : point[1] + 0.4;
            double xmin = point[0] - 0.5, xmax = point[0] + 0.5;
            var s = GetRBMArray(p_lesser, type);
            List<string> ll = new List<string>();
            List<int> ll_label = new List<int>();
            List<string> rbmPos = new List<string>();
            bool between(double xy, double r1, double r2) => (xy >= r1 && xy <= r2) || (xy >= r2 && xy <= r1);
            foreach (var i in s)
            {
                var a = i.Value;
                double t;
                if (a.Length == 2)
                    t = (a[0][0] - a[1][0]) * (ymax - a[1][1]) / (a[0][1] - a[1][1]) + a[1][0];
                else
                {
                    int u;
                    if (between(ymax, a[1][1], a[0][1]))
                        u = 0;
                    else // if between(a[1][1], a[2][1])
                        u = 2;
                    t = (a[u][0] - a[1][0]) * (ymax - a[1][1]) / (a[u][1] - a[1][1]) + a[1][0];
                }
                if (between(t, xmax, xmin))
                {
                    List<string> l = new List<string>();
                    foreach (var j in i.Value)
                    {
                        l.Add("[" + j[0].ToString("f4") + "," + j[1].ToString("f4") + "]");
                    }
                    ll.Add("[" + string.Join(",", l) + "]");
                    ll_label.Add(i.Key);
                    rbmPos.Add(t.ToString("f4"));
                }
            }
            return new Tuple<string, string, string>(
                "[" + string.Join(",", ll) + "]",
                "[" + string.Join(",", ll_label) + "]", 
                "[" + string.Join(",", rbmPos) + "]"
                );
        }
        public string YAxisLabel() => @"\(" + p1Arr_raw[p1_lesser + 1] + "-" + p1Arr_raw[p1_lesser] + @"\ (\mathrm{eV})\)";
        public string XAxisLabel() => @"\((" + p1Arr_raw[p1_lesser + 1] + "+" + p1Arr_raw[p1_lesser] + @")/2\ (\mathrm{eV})\)";
        public string IsMetal() => Energy.IsMetal(p_lesser) ? "true" : "false";
        
    }
}