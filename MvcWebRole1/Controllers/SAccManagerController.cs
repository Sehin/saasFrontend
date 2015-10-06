using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class SAccManagerController : Controller
    {
        //
        // GET: /SAccManager/
        DatabaseContext db = new DatabaseContext();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult GetGroupsPartial(int ID_AC)
        {
            IEnumerable<Group> groups = db.Groups.Where(g => g.ID_AC == ID_AC);
            return PartialView(groups);
        }
        public PartialViewResult AddGroupPartial(int ID_AC)
        {
            return PartialView(ID_AC);
        }

        public ActionResult GetFbSocInfoPartial()
        {
            int userId = getUserId();
            IEnumerable<SocAccount> socAccs = db.SocAccounts.Where(s => s.SOCNET_TYPE == 1).Where(u => u.ID_USER == userId);
            return PartialView(socAccs);
        }

        public ActionResult GetVkSocInfoPartial()
        {
            int userId = getUserId();
            SocAccount socAcc = db.SocAccounts.Where(s => s.SOCNET_TYPE == 0).Where(u => u.ID_USER == userId).Single();
            return PartialView(socAcc);
        }

        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }
    }
}
