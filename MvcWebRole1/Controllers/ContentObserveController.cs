using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class ContentObserveController : Controller
    {
        //
        // GET: /ContentObserve/
        DatabaseContext db = new DatabaseContext();
       
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult FilterTagsPartial()
        {
            List<String> allTags = new List<String>();
            int userId = getUserId();
            Content[] contents = db.Contents.Where(c => c.ID_USER == userId).ToArray();
            foreach (Content content in contents)
            {
                TagsInContent[] tags = db.TagsInContents.Where(t => t.ID_CO == content.ID_CO).ToArray();
                foreach (TagsInContent tag in tags)
                {
                    String tagTemp = db.Tags.Where(t => t.id == tag.ID_TAG).Single().name;
                    if (!allTags.Contains(tagTemp))
                    {
                        allTags.Add(tagTemp);
                    }
                }
            }
          
            return PartialView(allTags);
        }
        /*public ActionResult LoadContentPartial(String[] tags)
        {
            int userId = getUserId();
            List<int> tagIds = new List<int>();
            foreach (String tag in tags)
            {
               tagIds.Add(db.Tags.Where(t=>t.name==tag).Single().id);
            }
            List<int> contentIds = new List<int>();
            foreach(int tagId in tagIds)
            {
                int contentId = db.TagsInContents.Where(t => t.ID_TAG == tagId).Single().ID_CO;
                if (!contentIds.Contains(contentId))
                    contentIds.Add(contentId);
            }
            List<ContentView> contentViews = new List<ContentView>();
            foreach (int contentId in contentIds)
            {
                Content cnt = db.Contents.Where(c => c.ID_CO == contentId).Single();
                ContentData[] contentDatas = db.ContentDatas.Where(cd=>cd.ID_CO==contentId).ToArray();
                contentViews.Add(new ContentView(cnt.CONTENT_TEXT, cnt.CONTENT_TITLE, db.ContentDatas.Where(cd => cd.ID_CO == contentId).ToArray(), contentId));
            }
            return PartialView(contentViews);
        }*/

        public ActionResult LoadContentPartial(String tag)
        {
            int userId = getUserId();
            int tagId = db.Tags.Where(t => t.name == tag).Single().id;

            List<int> contentIds = new List<int>();
            List<TagsInContent> tic = db.TagsInContents.Where(t => t.ID_TAG == tagId).ToList();
            foreach (TagsInContent t in tic)
            {
                contentIds.Add(t.ID_CO);
            }

            List<ContentView> contentViews = new List<ContentView>();
            foreach (int contentId in contentIds)
            {
                Content cnt = db.Contents.Where(c => c.ID_CO == contentId).Single();
                ContentData[] contentDatas = db.ContentDatas.Where(cd => cd.ID_CO == contentId).ToArray();
                List<String> tags = new List<string>();
                List<TagsInContent> tagsInC = db.TagsInContents.Where(t => t.ID_CO == contentId).ToList();
                foreach (TagsInContent tagInC in tagsInC)
                {
                    String tagName = db.Tags.Where(t => t.id == tagInC.ID_TAG).Single().name;
                    if (!tags.Contains(tagName))
                    {
                        tags.Add(tagName);
                    }
                }
                contentViews.Add(new ContentView(cnt.CONTENT_TEXT, cnt.CONTENT_TITLE, db.ContentDatas.Where(cd => cd.ID_CO == contentId).ToArray(), contentId,tags.ToArray()));
            }
            return PartialView(contentViews);
        }
        public int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }

    }
}
