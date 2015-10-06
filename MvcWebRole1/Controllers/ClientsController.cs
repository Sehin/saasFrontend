using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class ClientsController : Controller
    {
        //
        // GET: /Clients/
        DatabaseContext db = new DatabaseContext();
        public ActionResult Index()
        {
            return View();
        }

    [HttpPost]
        // По запросу обновление 
        public void getGroupSubscribers()
        {
            List<Group> groups = db.Groups.ToList(); // получение списка групп
            foreach(Group gr in groups)
            {
                int socnet_type = db.SocAccounts.Where(u => u.ID_AC == gr.ID_AC).Single().SOCNET_TYPE;
                if (socnet_type==0) //если это VK
                {
                    
                }
                if (socnet_type == 1) //если это FB
                {

                }
            }
        }

    }
}
