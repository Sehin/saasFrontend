using MvcWebRole1.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class SocialController : Controller
    {
        DatabaseContext db = new DatabaseContext();
        public ActionResult Index()
        {
            return View();
        }

        public List<SocAccount> getVkAccounts()
        {
            List<SocAccount> list = new List<SocAccount>();
            int userId = getUserId();
            SocAccount[] sa = db.SocAccounts.Where(s => s.ID_USER == userId).Where(s => s.SOCNET_TYPE == 0).ToArray();
            for (int i = 0; i < sa.Length; i++) list.Add(sa[i]);
            return list;
        }

        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }

        public bool deleteGroup(int groupID)
        {
            var toDelete = db.Groups.Find(groupID);
            db.Groups.Remove(toDelete);
            db.SaveChanges();
            return true;
        }

        public bool addGroup(int socId, string groupId)
        {
            Group group = new Group(groupId, socId);
            db.Groups.Add(group);
            db.SaveChanges();
            return true;
        }

        public bool deleteAcc(int accID)
        {
            try
            {
                var toDeleteById = db.SocAccounts.Find(accID);
                db.SocAccounts.Remove(toDeleteById);
                db.SaveChanges();
                return true;
            }
            catch(Exception)
            { return false; }
        }
    }
}
