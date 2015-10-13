using MvcWebRole1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class VkontakteController : Controller
    {
        //
        // GET: /Vkontakte/

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        String client_id = "5070823";
        String redirect_uri = "http://localhost:65413/Vkontakte/getToken/";
        String client_secret = "7LCpt00ZrUCThUwyyYum";
        String api_version = "5.33";
        String[] scopeParams;
        DatabaseContext db = new DatabaseContext();

        public RedirectResult Auth()
        {
            #region scopeParams
            scopeParams = new String[6];
            scopeParams[0] = "friends";
            scopeParams[1] = "groups";
            scopeParams[2] = "stats";
            scopeParams[3] = "ads";
            scopeParams[4] = "offline";
            scopeParams[5] = "wall";
            #endregion
            String scopeParamsString = "";
            for (int i = 0; i < scopeParams.Length; i++)
            {
                if (i != scopeParams.Length - 1)
                {
                    scopeParamsString += scopeParams[i] + ",";
                }
                else
                {
                    scopeParamsString += scopeParams[i];
                }
            }
            return new RedirectResult("https://oauth.vk.com/authorize?client_id=" + client_id + "&scope=" + scopeParamsString + "&response_type=code");
        }

        public RedirectResult getToken(String url)
        {
            String code = url.Split('=')[1];
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String test = "https://oauth.vk.com/access_token?client_id=" + client_id + "&client_secret=" + client_secret + "&code=" + code + "&redirect_uri=" + redirect_uri;
            String access_token = wc.DownloadString("https://oauth.vk.com/access_token?client_id=" + client_id + "&client_secret=" + client_secret + "&code=" + code);
            JObject obj = JObject.Parse(access_token);
            String VkToken = (string)obj["access_token"];

            DatabaseContext db = new DatabaseContext();

            string login = HttpContext.User.Identity.Name;
            User user = db.Users.Where(u => u.Email == login).FirstOrDefault();
            SocAccount socAcc = new SocAccount(0, VkToken, user.Id);
            db.SocAccounts.Add(socAcc);
            db.SaveChanges();
            /*  if (user != null)
              {
                  db.Entry(socAcc).State = EntityState.Modified;
                  db.SaveChanges();
              }         */
            return RedirectPermanent("~/Tool/SocStudio/");
        }

        public String GetSocAccProfileImg(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?fields=photo_200_orig&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First["photo_200_orig"];
            return jtoken.ToString();
        }

        public String GetSocAccName(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken_f = obj["response"].First["first_name"];
            JToken jtoken_s = obj["response"].First["last_name"];
            return jtoken_f.ToString() + " " + jtoken_s.ToString();
        }

        public RedirectResult Delete()
        {
            int userId = getUserId();
            SocAccount sa = db.SocAccounts.Where(s => s.ID_USER == userId && s.SOCNET_TYPE == 0).Single();
            db.SocAccounts.Remove(sa);
            db.SaveChanges();
            return Redirect("/Tool/SocStudio");
        }
        public RedirectResult addGroup(string groupId)
        {
            int userId = getUserId();
            int accId = db.SocAccounts.Where(s => s.ID_USER == userId && s.SOCNET_TYPE == 0).Select(s => s.ID_AC).Single();
            Group group = new Group(groupId, accId);
            db.Groups.Add(group);
            db.SaveChanges();
            return Redirect("/Tool/SocStudio");
        }
        public RedirectResult deleteGroup(int groupId)
        {
            var group = db.Groups.Where(g => g.ID == groupId).Single();
            db.Groups.Remove(group);
            db.SaveChanges();
            return Redirect("/Tool/SocStudio");
        }
        private int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }

        public void testIt()
        {
        //    VKWorker.getNewsfeed();
        }
    }
    public static class VKWorker
    {
        public static List<int> getGroupSubscribersIds(int groupId)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/groups.getMembers?group_id=" + groupId);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"]["users"].First;

            List<int> ids = new List<int>();
            do
            {
                ids.Add((int)jtoken);
                jtoken = jtoken.Next;
            }
            while (jtoken != null);

            return ids;
        }
        public static String GetSocAccName(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken_f = obj["response"].First["first_name"];
            JToken jtoken_s = obj["response"].First["last_name"];
            return jtoken_f.ToString() + " " + jtoken_s.ToString();
        }
        public static String getGroupName(Group gr)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/groups.getById?group_id=" + gr.ID_GROUP);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First["name"];
            return jtoken.ToString();
        }
        public static String getGroupNameById(String id)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/groups.getById?group_id=" + id);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First["name"];
            return jtoken.ToString();
        }
        public static List<ClientComment> getSocialCommentsFromGroup(int ID_GROUP)
        {
            DatabaseContext db = new DatabaseContext();
            List<ClientComment> ccs = db.ClientComments
                 .Join(db.ContentsInGroups,
                 c => c.ID_CIG,
                 q => q.ID_CIG,
                 (c, q) => new { c, q }).Where(w => w.q.ID_GROUP == ID_GROUP).Select(v => v.c).ToList();
            return ccs;
        }
        public static String GetSocAccProfileImg(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?fields=photo_50&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First["photo_50"];
            return jtoken.ToString();
        }
        public static Tuple<String, String, String> getCommentData(ClientComment cc)
        {
            DatabaseContext db = new DatabaseContext();
            int ID_GROUP = db.ContentsInGroups.Join(db.ClientComments,
                c => c.ID_CIG,
                q => q.ID_CIG,
                (c, q) => new { c, q }).Select(w => w.c.ID_GROUP).First();
            String VK_ID_GR = db.Groups.Where(g => g.ID == ID_GROUP).Select(w => w.ID_GROUP).Single();

            int ID_POST = db.ContentsInGroups.Where(c => c.ID_CIG == cc.ID_CIG).Select(c => c.ID_POST).Single();



            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String test = "https://api.vk.com/method/wall.getComments?owner_id=-" + VK_ID_GR + "&post_id=" + ID_POST + "&start_comment_id=" + cc.ID_COMMENT + "&count=1&v=5.37";
            String answer = wc.DownloadString("https://api.vk.com/method/wall.getComments?owner_id=-" + VK_ID_GR + "&post_id=" + ID_POST + "&start_comment_id=" + cc.ID_COMMENT + "&count=1&v=5.37");
            JObject obj = JObject.Parse(answer);
            String text = obj["response"]["items"].First["text"].ToString();

            String clientName = db.Clients.Where(c => c.ID_CL == cc.ID_CL).Select(c => c.NAME).Single();
            String client_ID_VK = db.Clients.Where(c => c.ID_CL == cc.ID_CL).Select(c => c.ID_VK).Single();
            answer = wc.DownloadString("https://api.vk.com/method/users.get?user_ids=" + client_ID_VK + "&fields=photo_50");
            obj = JObject.Parse(answer);
            String photoUrl = obj["response"].First["photo_50"].ToString();



            return new Tuple<string, string, string>(photoUrl, clientName, text);
        }
        public static List<Tuple<String, String>> getAdmGroups(int userId)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            DatabaseContext db = new DatabaseContext();
            int idAcc = db.SocAccounts.Where(s => s.ID_USER == userId && s.SOCNET_TYPE == 0).Select(s => s.ID_AC).Single();
            var admGroups = db.Groups.Where(g => g.ID_AC == idAcc).Select(g => g.ID_GROUP).ToList();


            String token = db.SocAccounts.Where(s => s.ID_USER == userId && s.SOCNET_TYPE == 0).Select(s => s.TOKEN).Single();
            String answer = wc.DownloadString("https://api.vk.com/method/groups.get?filter=admin&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First;
            List<String> ids = new List<String>();
            do
            {
                ids.Add((String)jtoken);
                jtoken = jtoken.Next;
            }
            while (jtoken != null);
            List<Tuple<String, String>> groups = new List<Tuple<String, string>>();
            foreach (String id in ids)
            {
                if (!admGroups.Contains(id))
                    groups.Add(new Tuple<String, string>(id, getGroupNameById(id)));
            }
            return groups;
        }
        public static Newsfeed getNewsfeed(SocAccount sa)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;

            String answer = wc.DownloadString("https://api.vk.com/method/newsfeed.get?access_token=" + sa.TOKEN + "&max_photos=100");
            //String answer = wc.DownloadString("https://api.vk.com/method/newsfeed.get?access_token=ab6c5b495f5600f464bd18190be6de272deed88ab9dc0771f1925b4668ce7cc0265d6f9480751c580ff5c&max_photos=100");
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"]["items"].First;
            Newsfeed newsfeed = new Newsfeed();
            //newsfeed
            do
            {
                switch (jtoken["type"].ToString())
                {
                    case "wall_photo":
                        WallPhoto wallphoto = new WallPhoto();
                        wallphoto.attach = getPhotoAttachments(jtoken);
                        wallphoto.date = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(int.Parse(jtoken["date"].ToString()));
                        wallphoto.idFrom = jtoken["source_id"].ToString();
                        newsfeed.feed.Add(wallphoto);
                        break;
                    case "post":
                        Post post = new Post();
                        if (jtoken["attachments"]!=null)
                            post.attach = getAttachments(jtoken);
                        post.date = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(int.Parse(jtoken["date"].ToString()));
                        post.idFrom = jtoken["source_id"].ToString();
                        post.text = jtoken["text"].ToString();
                        post.id = jtoken["post_id"].ToString();
                        newsfeed.feed.Add(post);
                        break;
                    case "photo":
                        Photo photo = new Photo();
                        photo.idFrom = jtoken["source_id"].ToString();
                        photo.date = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(int.Parse(jtoken["date"].ToString()));
                        photo.attach = getPhotoAttachments(jtoken);
                        newsfeed.feed.Add(photo);
                        break;
                    case "friend":
                        Friend friend = new Friend();
                        friend.date = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(int.Parse(jtoken["date"].ToString()));
                        friend.attach = getFriendAttachments(jtoken);
                        friend.idFrom = jtoken["source_id"].ToString();
                        newsfeed.feed.Add(friend);
                        break;
                    default:

                        break;
                }


                jtoken = jtoken.Next;
            }
            while (jtoken != null);

            //profiles
            jtoken = obj["response"]["profiles"].First;
            do
            {
                NFProfile profile = new NFProfile();
                profile.id = jtoken["uid"].ToString();
                profile.first_name = jtoken["first_name"].ToString();
                profile.last_name = jtoken["last_name"].ToString();
                profile.photo_url = jtoken["photo"].ToString();

                newsfeed.profiles.Add(profile);
                jtoken = jtoken.Next;
            }
            while (jtoken != null);


            jtoken = obj["response"]["groups"].First;
            do
            {
                NFGroup group = new NFGroup();
                group.id = jtoken["gid"].ToString();
                group.name = jtoken["name"].ToString();
                group.screen_name = jtoken["screen_name"].ToString();
                group.photo_url = jtoken["photo"].ToString();

                newsfeed.groups.Add(group);
                jtoken = jtoken.Next;
            }
            while (jtoken != null);


            return newsfeed;
        }

        private static List<IAttachments> getPhotoAttachments(JToken token)
        {
            List<IAttachments> attachments = new List<IAttachments>();
            JToken jtoken = token["photos"].First.Next;
            do
            {
                PhotoAttach pa = new PhotoAttach(jtoken["owner_id"].ToString(), jtoken["pid"].ToString(), jtoken["src"].ToString());
                attachments.Add(pa);
                jtoken = jtoken.Next;
            }
            while (jtoken != null);
            return attachments;

        }
        private static List<IAttachments> getFriendAttachments(JToken token)
        {
            List<IAttachments> attachments = new List<IAttachments>();
            JToken jtoken = token["friends"].First.Next;
            do
            {
                FriendAttach fa = new FriendAttach();
                fa.id = jtoken["uid"].ToString();
                fa.owner_id = jtoken["uid"].ToString();
                attachments.Add(fa);
                jtoken = jtoken.Next;
            }
            while (jtoken != null);
            return attachments;

        }
        private static List<IAttachments> getAttachments(JToken token)
        {
            List<IAttachments> attachments = new List<IAttachments>();

            JToken jtoken = token["attachments"].First;
            do
            {
                switch (jtoken["type"].ToString())
                {
                    case "photo":
                        PhotoAttach pa = new PhotoAttach(jtoken["photo"]["owner_id"].ToString(), jtoken["photo"]["pid"].ToString(), jtoken["photo"]["src"].ToString());
                        attachments.Add(pa);
                        break;
                    case "video":
                        VideoAttach va = new VideoAttach(jtoken["video"]["owner_id"].ToString(), jtoken["video"]["vid"].ToString(), jtoken["video"]["title"].ToString(), jtoken["video"]["description"].ToString());
                        attachments.Add(va);
                        break;
                    case "audio":
                        AudioAttach aa = new AudioAttach(jtoken["audio"]["owner_id"].ToString(), jtoken["audio"]["aid"].ToString(), jtoken["audio"]["artist"].ToString(), jtoken["audio"]["title"].ToString(), jtoken["audio"]["url"].ToString());
                        attachments.Add(aa);
                        break;
                }
                jtoken = jtoken.Next;
            }
            while (jtoken != null);

            return attachments;
        }
    }

    public class Newsfeed
    {
        public List<IVKPost> feed = new List<IVKPost>();
        public List<NFProfile> profiles = new List<NFProfile>();
        public List<NFGroup> groups = new List<NFGroup>();

    }
    public interface IVKPost
    {
        String idFrom { get; set; }
        String id { get; set; }
        DateTime date { get; set; }
        List<IAttachments> attach { get; set; }
    }
    public class Post : IVKPost
    {
        public String id { get; set; }
        public String idFrom { get; set; }
        public DateTime date { get; set; }
        public String text { get; set; }
        public List<IAttachments> attach { get; set; }
    }
    public class WallPhoto : IVKPost
    {
        public String id { get; set; }
        public String idFrom { get; set; }
        public DateTime date { get; set; }
        public List<IAttachments> attach { get; set; }
    }
    public class Photo : IVKPost
    {
        public String id { get; set; }
        public String idFrom { get; set; }
        public DateTime date { get; set; }
        public List<IAttachments> attach { get; set; }
        public String photo_maxSize_url { get; set; }
    }
    public class Friend : IVKPost
    {
        public String idFrom { get; set; }
        public String id { get; set; }
        public DateTime date { get; set; }
        public List<IAttachments> attach { get; set; }
    }
    public interface IAttachments
    {
        String owner_id { get; set; }
        String id { get; set; }
    }
    public class PhotoAttach : IAttachments
    {
        public String owner_id { get; set; }
        public String id { get; set; }
        public String photo_maxSize_url { get; set; }
        public PhotoAttach(String owner_id, String id, String photo_maxSize_url)
        {
            this.owner_id = owner_id;
            this.id = id;
            this.photo_maxSize_url = photo_maxSize_url;
        }
    }
    public class VideoAttach : IAttachments
    {
        public String owner_id { get; set; }
        public String id { get; set; }
        public String title { get; set; }
        public String description { get; set; }
        public VideoAttach(String owner_id, String id, String title, String description)
        {
            this.owner_id = owner_id;
            this.id = id;
            this.title = title;
            this.description = description;
        }
    }
    public class AudioAttach : IAttachments
    {
        public String owner_id { get; set; }
        public String id { get; set; }
        public String artist { get; set; }
        public String title { get; set; }
        public String url { get; set; }
        public AudioAttach(String owner_id, String id, String artist, String title, String url)
        {
            this.owner_id = owner_id;
            this.id = id;
            this.artist = artist;
            this.title = title;
            this.url = url;
        }
    }
    public class FriendAttach : IAttachments
    {
        public String owner_id { get; set; }
        public String id { get; set; }
    }

    public class NFProfile
    {
        public String id;
        public String first_name;
        public String last_name;
        public String photo_url;
    }
    public class NFGroup
    {
        public String id;
        public String name;
        public String screen_name;
        public String photo_url;
    }
}
