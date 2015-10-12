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
        public PartialViewResult VkGroupPartial(int vkGroup)
        {
            Group gr = db.Groups.Find(vkGroup);
            return PartialView(gr);
        }
        public PartialViewResult VkGroupsPartial()
        {
            int userId = getUserId();
            int sa_id = db.SocAccounts.Where(s => s.ID_USER == userId && s.SOCNET_TYPE == 0).Select(s => s.ID_AC).Single();
            List<Group> groups = db.Groups.Where(g => g.ID_AC == sa_id).ToList();
            return PartialView(groups);
        }
        public PartialViewResult VkPostAttachmentPartial(List<IAttachments> attachment)
        {
            return PartialView(attachment);
        }
        public PartialViewResult VkFeedPartial()
        {
            int userId = getUserId();
            SocAccount sa = db.SocAccounts.Where(s => s.ID_USER == userId && s.SOCNET_TYPE == 0).Single();
            Newsfeed newsfeed = VKWorker.getNewsfeed(sa);
            return PartialView(newsfeed);
        }
        public PartialViewResult VkPostPartial(IProfile profile)
        {
            return PartialView(profile);
        }
        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }
    }
}
