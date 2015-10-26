using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class ContentViewController : Controller
    {
        DatabaseContext db = new DatabaseContext();
        MyBlobStorageService myBlobStorageService = new MyBlobStorageService();
        
        public ActionResult Index(int contentId = -1)
        {
            if (contentId != -1)
            {
                try
                {
                    Content content = db.Contents.Where(c => c.ID_CO == contentId).Single();
                    List<ContentData> contentDatas = db.ContentDatas.Where(cd => cd.ID_CO == content.ID_CO).ToList();
                    List<String> tagsNames = new List<String>();
                    TagsInContent[] tags = db.TagsInContents.Where(t => t.ID_CO == contentId).ToArray();
                    foreach (TagsInContent tagsInC in tags)
                    {
                        Tag tag = db.Tags.Where(t => t.id == tagsInC.ID_TAG).Single();
                        tagsNames.Add(tag.name);
                    }
                    ContentView cw = new ContentView(content.CONTENT_TEXT, content.CONTENT_TITLE, contentDatas.ToArray(), contentId, tagsNames.ToArray());
                    return View(cw);
                }
                catch (System.InvalidOperationException)
                {
                    return View();
                }
            }
            else
                return View();
        }
        public PartialViewResult ContentPanelPartial()
        {
            List<ContentView> contentViews = new List<ContentView>();
            int userId = getUserId();
            List<Content> contents = db.Contents.Where(c => c.ID_USER == userId).ToList();
            foreach (Content content in contents)
            {
                List<ContentData> contentDatas = db.ContentDatas.Where(cd => cd.ID_CO == content.ID_CO).ToList();
                contentViews.Add(new ContentView(content.CONTENT_TEXT, content.CONTENT_TITLE, contentDatas.ToArray(), content.ID_CO));
            }
            return PartialView(contentViews);
        }     
        public void DeleteImage(int contentId)
        {
            ContentData cd = db.ContentDatas.Where(c => c.ID_CD == contentId).Single();
            String name = cd.URL.Split('/')[4];
            CloudBlockBlob blob = myBlobStorageService.getCloudBlobContainer().GetBlockBlobReference(name); //new CloudBlockBlob(new Uri(cd.URL));
            blob.Delete();

            db.ContentDatas.Remove(cd);
            db.SaveChanges();     
        }    
        [HttpPost]
        public ActionResult AddContent(HttpPostedFileBase[] fileBase, String text, String title, String tags, int ContentId=-1)
        {
            Content content;
            if (ContentId != -1)
            {
                content = db.Contents.Where(c => c.ID_CO == ContentId).Single();
                content.CONTENT_TEXT = text;
                content.CONTENT_TITLE = title;
                TagsInContent[] tic = db.TagsInContents.Where(t => t.ID_CO == ContentId).ToArray();
                foreach (TagsInContent t in tic)
                {
                    db.TagsInContents.Remove(t);
                }
                db.SaveChanges();
            }
            else
            {
                if (fileBase.Length > 0)
                    content = new Content(getUserId(), text, title, 0);
                else
                    content = new Content(getUserId(), text, title, 1);
                db.Contents.Add(content);
                db.SaveChanges();
            }
            String[] _tags = tags.Split(',');
            // Проверка существует ли данный тег, если нет - то добавить
            for (int i = 0; i < _tags.Length;i++ )
            {
                try 
                {
                    String tagName = _tags[i];
                    Tag tag = db.Tags.Where(t => t.name == tagName).Single();
                    TagsInContent tic = new TagsInContent(content.ID_CO, tag.id);
                    
                    db.TagsInContents.Add(tic);
                    db.SaveChanges();
                }

                catch(Exception) // Не существует
                {
                    Tag tag = new Tag(_tags[i]);
                    db.Tags.Add(tag);
                    db.SaveChanges();
                    TagsInContent tic = new TagsInContent(content.ID_CO, tag.id);
                    db.TagsInContents.Add(tic);
                    db.SaveChanges();
                }
            }

            for (int i = 0; i < fileBase.Length; i++)
            {
                PostImage(fileBase[i], content.ID_CO);
            }
            return RedirectToAction("Index", new { contentId = content.ID_CO });
        }        
        public String PostImage(HttpPostedFileBase fileBase, int content_id)
        {
            if (fileBase != null)
            {
                if (fileBase.ContentLength > 0)
                {
                    CloudBlobContainer container = myBlobStorageService.getCloudBlobContainer();
                    CloudBlockBlob blob = container.GetBlockBlobReference(String.Format("img_{0}.jpg", DateTime.Now.ToString("yyyyMMddHHmmssfff")));
                    blob.UploadFromStream(fileBase.InputStream);
                    ContentData cd = new ContentData(content_id, blob.Uri.ToString(), 0);
                    db.ContentDatas.Add(cd);
                    db.SaveChanges();
                    return blob.Uri.ToString();
                }
            }
            return null;
        }        
        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }
        public ActionResult DeleteContent(int idContent)
        {
            Content content = db.Contents.Where(c => c.ID_CO == idContent).Single();

            List<ContentData> _contentDatas = db.ContentDatas.Where(c => c.ID_CO == idContent).ToList();
            foreach(ContentData cd in _contentDatas) // Удаление из storage
            {
                CloudBlockBlob blob = new CloudBlockBlob(new Uri(cd.URL));
                blob.Delete();
            }

            db.Contents.Remove(content);
            db.SaveChanges();

            List<ContentView> contentViews = new List<ContentView>();
            int userId = getUserId();
            List<Content> contents = db.Contents.Where(c => c.ID_USER == userId).ToList();
            foreach (Content _content in contents)
            {
                List<ContentData> contentDatas = db.ContentDatas.Where(cd => cd.ID_CO == _content.ID_CO).ToList();
                contentViews.Add(new ContentView(_content.CONTENT_TEXT, _content.CONTENT_TITLE, contentDatas.ToArray(), _content.ID_CO));
            }
            return RedirectToAction("Index");
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
