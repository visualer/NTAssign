﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Optimization;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using NTAssign;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Home/_Calculator.cshtml")]
    public partial class _Views_Home__Calculator_cshtml_ : System.Web.Mvc.WebViewPage<NTAssign.Models.AssignModel>
    {
        public _Views_Home__Calculator_cshtml_()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n");

            
            #line 4 "..\..\Views\Home\_Calculator.cshtml"
   
    var _model = Model.Calculator();

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n<table");

WriteLiteral(" class=\"table table-striped math\"");

WriteLiteral(" id=\"calcResultList\"");

WriteLiteral(" style=\"margin-left: 10%\"");

WriteLiteral(">\r\n");

            
            #line 9 "..\..\Views\Home\_Calculator.cshtml"
    
            
            #line default
            #line hidden
            
            #line 9 "..\..\Views\Home\_Calculator.cshtml"
     if (!(_model is null))
    {

            
            #line default
            #line hidden
WriteLiteral("        <tr>\r\n            <td>\\(\\omega_\\mathrm{RBM} ");

            
            #line 12 "..\..\Views\Home\_Calculator.cshtml"
                                  Write(Model.Env == 0 ? @"\ (p=1)" : "");

            
            #line default
            #line hidden
WriteLiteral(" \\)</td>\r\n            <td>\\(");

            
            #line 13 "..\..\Views\Home\_Calculator.cshtml"
             Write(Energy.DttoRBM(Energy.Dt(Model.NCalc, Model.MCalc, Model.Env), 0, Model.Env).ToString("f1"));

            
            #line default
            #line hidden
WriteLiteral("\r\n            \\ \\mathrm{cm^{-1}}\\)</td>\r\n        </tr>\r\n");

            
            #line 16 "..\..\Views\Home\_Calculator.cshtml"
        for (int i = 0; i < _model.Item1.Count; i++)
        {

            
            #line default
            #line hidden
WriteLiteral("            <tr>\r\n                <td>");

            
            #line 19 "..\..\Views\Home\_Calculator.cshtml"
               Write(NTAssign.Energy.p1Arr[_model.Item1[i]]);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                <td>\\(");

            
            #line 20 "..\..\Views\Home\_Calculator.cshtml"
                 Write(_model.Item2[i].ToString("f3"));

            
            #line default
            #line hidden
WriteLiteral(" \\ \\mathrm{eV}\\)</td>\r\n            </tr>\r\n");

            
            #line 22 "..\..\Views\Home\_Calculator.cshtml"
        }
    }

            
            #line default
            #line hidden
WriteLiteral("</table>\r\n");

        }
    }
}
#pragma warning restore 1591
