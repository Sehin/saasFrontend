using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class ContentCreatorController : Controller
    {
        //
        // GET: /ContentCreator/
        DatabaseContext db = new DatabaseContext();
        MyBlobStorageService myBlobStorageService = new MyBlobStorageService();

        public ActionResult Index(int contentId=-1)
        {
            if (contentId == -1)
                return View();
            Content content = db.Contents.Where(c => c.ID_CO == contentId).Single();
            ContentData[] contentDatas = db.ContentDatas.Where(cd => cd.ID_CO==contentId).ToArray();
            List<String> urls = new List<String>();
            foreach (ContentData cnt in contentDatas)
            {
                urls.Add(cnt.URL);
            }
            ContentView cw = new ContentView(content.CONTENT_TEXT, content.CONTENT_TITLE, urls.ToArray(), contentId);
            return View(cw);
        }

        [HttpPost]
        public ActionResult PostContent(HttpPostedFileBase[] fileBase, String text, String title)
        {
            Content content = new Content(getUserId(), text, title);
            db.Contents.Add(content);
            db.SaveChanges();
            for (int i = 0; i < fileBase.Length;i++ )
            {
                PostImage(fileBase[i], content.ID_CO);
            }
            return RedirectToAction("Index");
        }
        public String PostImage(HttpPostedFileBase fileBase, int content_id)
        {
            if (fileBase.ContentLength > 0)
            {
                CloudBlobContainer container = myBlobStorageService.getCloudBlobContainer();
                CloudBlockBlob blob = container.GetBlockBlobReference(String.Format("img_{0}.jpg", DateTime.Now.ToString("yyyyMMddHHmmssfff")));
                blob.UploadFromStream(fileBase.InputStream);
                ContentData cd = new ContentData(content_id, blob.Uri.ToString(),0);
                db.ContentDatas.Add(cd);
                db.SaveChanges();
                return blob.Uri.ToString();
            }
            return null;
        }
        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }

    }
    public class MyBlobStorageService
    {
        public CloudBlobContainer getCloudBlobContainer()
        {
            String containerName = "pictures";
            String connectionString = ConfigurationManager.ConnectionStrings["AzureStorageAccount"].ConnectionString;
            CloudStorageAccount sa = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient bc = sa.CreateCloudBlobClient();
            CloudBlobContainer container = bc.GetContainerReference(containerName);
            return container;
        }
    }
}
