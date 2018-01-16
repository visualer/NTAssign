using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static NTAssign.Energy;

namespace NTAssign.Controllers
{
    public class HomeController : Controller
    {
        private static List<SelectListItem> SLGen(string[] x, int selected = -1, int? threshold = null)
        {
            List<SelectListItem> sl = new List<SelectListItem>();
            for (int i = 0; i < (threshold ?? x.Length); i++)
                sl.Add(new SelectListItem() { Value = i.ToString(), Text = x[i] });
            if (selected > -1)
                sl[selected].Selected = true;
            return sl;
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Step1()
        {
            ViewBag.slEnv = SLGen(envArr, 0);
            return View();
        }
        
        public ActionResult Step2()
        {
            return RedirectToAction("Step1");
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Step2(Models.AssignModel l)
        {
            if (ModelState.IsValid)
            {
                ViewBag.slEnv = SLGen(envArr);
                ViewBag.slType = SLGen(p1Arr, -1, (l.Env <= 2) ? (int?)null : (l.Env == 3) ? 8 : 2);
                return View(l);
            }
            else
                ModelState.AddModelError("Invalid Input", new UnauthorizedAccessException());
            return RedirectToAction("Step1");
        }
        public ActionResult Step3()
        {
            return RedirectToAction("Step1");
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Step3(Models.AssignModel i)
        {
            if (ModelState.IsValid)
            {
                i.GetPlotModel(out Models.PlotModel[] pm);
                if (pm.Length == 1)
                    return View(pm[0]);
                else
                    throw new NotImplementedException();
                    // return View("Step3_Dual", pm);
            }
            ModelState.AddModelError("Invalid Input", new Exception());
            return RedirectToAction("Step1");
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public PartialViewResult Calculator(Models.AssignModel l)
        {
            return PartialView("_Calculator", l);
        }
    }
}