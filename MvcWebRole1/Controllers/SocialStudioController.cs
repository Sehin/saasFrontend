using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class SocialStudioController : Controller
    {
        //
        // GET: /SocialStudio/
        DatabaseContext db = new DatabaseContext();

        public ActionResult Index()
        {           
            return View(getUserId());
        }
        public PartialViewResult VkPartial(List<Group> groups)
        {
            return PartialView(groups);
        }
        public List<ClientComment> getSocialCommentsFromGroup(int ID_GROUP)
        {
            List<ClientComment> ccs = (List<ClientComment>)db.ClientComments
                 .Join(db.ContentsInGroups,
                 c => c.ID_CIG,
                 q => q.ID_CIG,
                 (c, q) => new { c, q }).Where(w => w.q.ID_GROUP == ID_GROUP);
           return ccs;
        }
        public void getTopLikerFromGroup(int ID_GROUP)
        {
            var client = db.ClientLikes
                .Join(db.ContentsInGroups,
                c => c.ID_CIG,
                q => q.ID_CIG,
                (c, q) => new { c, q }).Max(qq => qq.q);
            
        }

       
        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).Select(u => u.Id).Single();
        }

    }
}
