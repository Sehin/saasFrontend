using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class ToolController : Controller
    {
        //
        // GET: /Tool/

        DatabaseContext db = new DatabaseContext();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SocStudio()
        {
            return View(getUserId());
        }

        public PartialViewResult VkPartial(List<Group> gr)
        {
            return PartialView(gr);
        }
        public PartialViewResult VkGroupPartial(Group gr)
        {
            return PartialView(gr);
        }

        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }
    }
}
