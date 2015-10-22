﻿using MvcWebRole1.Models;
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
            scopeParams = new String[7];
            scopeParams[0] = "friends";
            scopeParams[1] = "groups";
            scopeParams[2] = "stats";
            scopeParams[3] = "ads";
            scopeParams[4] = "offline";
            scopeParams[5] = "wall";
            scopeParams[6] = "photos";
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
        /*type:
         * 0 - photo
         * 1 - photo comment
         * 2 - ..
         * 
         */
        
        public int Like(String id, String owner_id, int type)
        {
            int userId = getUserId();
            String token = db.SocAccounts.Where(s=>s.SOCNET_TYPE==0 && s.ID_USER==userId).Select(s=>s.TOKEN).Single();
            return VKWorker.Like(id, owner_id, token,type);
        }
        public int Unlike(String id, String owner_id, int type)
        {
            int userId = getUserId();
            String token = db.SocAccounts.Where(s => s.SOCNET_TYPE == 0 && s.ID_USER == userId).Select(s => s.TOKEN).Single();
            return VKWorker.imageUnlike(id, owner_id, token, type);
        }
        
        private int getUserId()
        {
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }

        public VkCommentsViewModel GetImageComments(String id, String owner_id, int offset, String access_key)
        {
            int userId = getUserId();
            String token = db.SocAccounts.Where(s => s.SOCNET_TYPE == 0 && s.ID_USER == userId).Select(s => s.TOKEN).Single();
            return VKWorker.getImageComments(id, owner_id, offset, token, access_key);
        }

        public void testIt()
        {
            //VKWorker.getNewsfeed();
        }
    }
    public static class VKWorker
    {
        public static int Like(String id, String owner_id, String token, int type)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String type_ = "";
            switch (type)
            {
                case 0:
                    type_ = "photo";
                    break;
                case 1:
                    type_ = "photo_comment";
                    break;
            }

            // Check is in Like List already
            String answer = wc.DownloadString("https://api.vk.com/method/likes.isLiked?type=" + type_ + "&owner_id=" + owner_id + "&item_id=" + id + "&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            if(obj["response"].ToString().Equals("0"))  // Not Liked
            {
                answer = wc.DownloadString("https://api.vk.com/method/likes.add?type=" + type_ + "&owner_id=" + owner_id + "&item_id=" + id + "&access_token=" + token);
                obj = JObject.Parse(answer);
                JToken jtoken = obj["response"]["likes"];
                return int.Parse(jtoken.ToString());
            }
            else
            {
                answer = wc.DownloadString("https://api.vk.com/method/likes.delete?type=" + type_ + "&owner_id=" + owner_id + "&item_id=" + id + "&access_token=" + token);
                obj = JObject.Parse(answer);
                JToken jtoken = obj["response"]["likes"];
                return int.Parse(jtoken.ToString());
            }     
        }
        public static int imageUnlike(String id, String owner_id, String token, int type)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String type_ = "";
            switch (type)
            {
                case 0:
                    type_ = "photo";
                    break;
                case 1:
                    type_ = "photo_comment";
                    break;
            }
            String answer = wc.DownloadString("https://api.vk.com/method/likes.delete?type="+type_+"&owner_id=" + owner_id + "&item_id=" + id + "&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"]["likes"];
            return int.Parse(jtoken.ToString());
        }
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
        public static List<Tuple<String, String>> getAdmGroups()
        {
            int userId = getUserId();
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


            #region profiles
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
            #endregion
            #region groups
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
            #endregion
            return newsfeed;
        }
        private static List<IAttachments> getPhotoAttachments(JToken token)
        {
            List<IAttachments> attachments = new List<IAttachments>();
            JToken jtoken = token["photos"].First.Next;
            do
            {
                String owner_id = jtoken["owner_id"].ToString();
                String id = jtoken["pid"].ToString();
                String access_key = jtoken["access_key"].ToString();
                PhotoAttach pa = new PhotoAttach(owner_id, id, jtoken["src_big"].ToString(),access_key);
                attachments.Add(pa);
                jtoken = jtoken.Next;
            }
            while (jtoken != null);
            return attachments;

        }

        public static Tuple<int, bool, String> getVideoInfo(String id, String owner_id, String token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/video.get?videos=" + owner_id + "_" + id + "&access_token=" + token + "&extended=1");
            JObject obj = JObject.Parse(answer);
            int count = Int32.Parse(obj["response"].First["likes"]["count"].ToString());
            bool isLiked = false;
            String isLikedI = obj["response"].First["likes"]["user_likes"].ToString();
            if (isLikedI.Equals("1"))
                isLiked = true;
            String url = obj["response"].First["likes"]["user_likes"].ToString();

            Tuple<int, bool, String> info = new Tuple<int, bool, String>(count, isLiked, "");

            return info;
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
                        String owner_id = jtoken["photo"]["owner_id"].ToString();
                        String id = jtoken["photo"]["pid"].ToString();
                        String access_key = "";
                        if (jtoken["photo"]["access_key"] != null)
                        {
                            access_key = jtoken["photo"]["access_key"].ToString();
                        }
                        PhotoAttach pa = new PhotoAttach(owner_id, id, jtoken["photo"]["src_big"].ToString(),access_key);
                        attachments.Add(pa);
                        break;
                    case "video":
                        owner_id = jtoken["video"]["owner_id"].ToString();
                        id = jtoken["video"]["vid"].ToString();
                        VideoAttach va = new VideoAttach(owner_id, id, jtoken["video"]["title"].ToString(), jtoken["video"]["description"].ToString());
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
        public static List<Tuple<String,String,int,bool>> getImagesLikeInfo(String imagesFullName, String token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/photos.getById?photos=" + imagesFullName +"&access_token="+token+"&extended=1");
            JObject obj = JObject.Parse(answer);
            int count = Int32.Parse(obj["response"].First["likes"]["count"].ToString());
            bool isLiked = false;
            String isLikedI = obj["response"].First["likes"]["user_likes"].ToString();
            if (isLikedI.Equals("1"))
                isLiked = true;

            Tuple<int, bool> info = new Tuple<int, bool>(count, isLiked);

            return null;
        }
        public static Tuple<int, bool> getImageLikeInfo(String id, String owner_id, String token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/photos.getById?photos=" + owner_id + "_" + id + "&access_token=" + token + "&extended=1");
            JObject obj = JObject.Parse(answer);
            int count = Int32.Parse(obj["response"].First["likes"]["count"].ToString());
            bool isLiked = false;
            String isLikedI = obj["response"].First["likes"]["user_likes"].ToString();
            if (isLikedI.Equals("1"))
                isLiked = true;
            Tuple<int, bool> info = new Tuple<int, bool>(count, isLiked);

            return info;
        }
        public static VkCommentsViewModel getImageComments(String id, String owner_id, int offset, String token, String access_key)
        {
            VkCommentsViewModel cvm = new VkCommentsViewModel();
            cvm.owner_id = owner_id;
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/photos.getComments?owner_id=" + owner_id + "&photo_id=" + id + "&access_token=" + token + "&extended=1&offset=" + offset + "&access_key=" + access_key+"&v=5.37&need_likes=1&count=10");
            JObject obj = JObject.Parse(answer);
            #region comments
            JToken jtoken;
            if (!obj["response"]["count"].ToString().Equals("0"))
            {
                if (obj["response"]["items"].HasValues)
                {
                    jtoken = obj["response"]["items"].First;
                    List<Post> comments = new List<Post>();
                    do
                    {
                        Post post = new Post();
                        post.id = jtoken["id"].ToString();
                        post.idFrom = jtoken["from_id"].ToString();
                        post.date = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(int.Parse(jtoken["date"].ToString()));
                        post.text = jtoken["text"].ToString();
                        post.likeCount = int.Parse(jtoken["likes"]["count"].ToString());
                        String isLikedI = jtoken["likes"]["user_likes"].ToString();
                        if (isLikedI.Equals("1"))
                            post.isLiked = true;
                        else post.isLiked = false;

                        try
                        {
                            if (jtoken["attachments"].First.HasValues)
                            {
                                post.attach = getAttachments(jtoken);
                            }
                        }
                        catch (Exception e)
                        { }
                        comments.Add(post);
                        jtoken = jtoken.Next;
                    } while (jtoken != null);
                    cvm.comments = comments;
                }
            }
            #endregion
            #region profiles
            jtoken = obj["response"]["profiles"].First;
            if (jtoken != null)
            {
                do
                {
                    NFProfile profile = new NFProfile();
                    profile.id = jtoken["id"].ToString();
                    profile.first_name = jtoken["first_name"].ToString();
                    profile.last_name = jtoken["last_name"].ToString();
                    profile.photo_url = jtoken["photo_100"].ToString();

                    cvm.profiles.Add(profile);
                    jtoken = jtoken.Next;
                }
                while (jtoken != null);
            }
            #endregion
            #region groups
            jtoken = obj["response"]["groups"].First;
            if (jtoken != null)
            {
                do
                {
                    NFGroup group = new NFGroup();
                    group.id = jtoken["gid"].ToString();
                    group.name = jtoken["name"].ToString();
                    group.screen_name = jtoken["screen_name"].ToString();
                    group.photo_url = jtoken["photo"].ToString();

                    cvm.groups.Add(group);
                    jtoken = jtoken.Next;
                }
                while (jtoken != null);
            }
            #endregion
            return cvm;
        }
        private static int getUserId()
        {
            DatabaseContext db = new DatabaseContext();
            string login = HttpContext.Current.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }
    }

    public class Newsfeed
    {
        public List<IVKPost> feed = new List<IVKPost>();
        public List<NFProfile> profiles = new List<NFProfile>();
        public List<NFGroup> groups = new List<NFGroup>();

        public IProfile getProfileById(string id)
        {
            long id_ = Int64.Parse(id);
            if (id_ > 0)
            {
                foreach (NFProfile profile in profiles)
                {
                    if (profile.id.Equals(id))
                        return profile;
                }
            }
            else
            {
                id_ *= -1;
                foreach (NFGroup group in groups)
                {
                    if (group.id.Equals(id_.ToString()))
                        return group;
                }
            }



            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;

            

            String answer = wc.DownloadString("https://api.vk.com/method/users.get?user_ids=" + id+"&fields=photo_max");
            JObject obj = JObject.Parse(answer);
            NFProfile profile_ = new NFProfile();
            profile_.id = id;
            profile_.first_name = obj["response"].First["first_name"].ToString();
            profile_.last_name = obj["response"].First["last_name"].ToString();
            profile_.photo_url = obj["response"].First["photo_max"].ToString();

            return profile_;
        }

    }
    public interface IVKPost
    {
        String idFrom { get; set; }
        String id { get; set; }
        DateTime date { get; set; }
        int likeCount { get; set; }
        bool isLiked { get; set; }
        List<IAttachments> attach { get; set; }
    }
    public class Post : IVKPost
    {
        public String id { get; set; }
        public String idFrom { get; set; }
        public DateTime date { get; set; }
        public String text { get; set; }
        public int likeCount { get; set; }
        public bool isLiked { get; set; }
        public List<IAttachments> attach { get; set; }
    }
    public class WallPhoto : IVKPost
    {
        public String id { get; set; }
        public String idFrom { get; set; }
        public DateTime date { get; set; }
        public List<IAttachments> attach { get; set; }
        public int likeCount { get; set; }
        public bool isLiked { get; set; }
    }
    public class Photo : IVKPost
    {
        public String id { get; set; }
        public String idFrom { get; set; }
        public DateTime date { get; set; }
        public int likeCount { get; set; }
        public bool isLiked { get; set; }
        public List<IAttachments> attach { get; set; }
        public String photo_maxSize_url { get; set; }
    }
    public class Friend : IVKPost
    {
        public String idFrom { get; set; }
        public String id { get; set; }
        public DateTime date { get; set; }
        public int likeCount { get; set; }
        public bool isLiked { get; set; }
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
        public String group_id { get; set; }
        public String id { get; set; }
        public String photo_maxSize_url { get; set; }
        public int likeCount { get; set; }
        public bool isLiked { get; set; }
        public List<Post> comments { get; set; }
        public String access_key { get; set; }
        public PhotoAttach(String owner_id, String id, String photo_maxSize_url, String access_key)
        {
            this.owner_id = owner_id;
            this.id = id;
            this.photo_maxSize_url = photo_maxSize_url;
            this.access_key = access_key;
        }
    }
    public class VideoAttach : IAttachments
    {
        public String owner_id { get; set; }
        public String id { get; set; }
        public String title { get; set; }
        public String description { get; set; }
        public int likeCount { get; set; }
        public bool isLiked { get; set; }
        public List<Post> comments { get; set; }
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
    public interface IProfile
    {
        String id { get; set; }
        String photo_url { get; set; }
    }
    public class NFProfile : IProfile
    {
        public String id { get; set; }
        public String first_name { get; set; }
        public String last_name { get; set; }
        public String photo_url { get; set; }
    }
    public class NFGroup : IProfile
    {
        public String id { get; set; }
        public String name { get; set; }
        public String screen_name { get; set; }
        public String photo_url { get; set; }
    }
    public class VkImageViewModel
    {
        public String id;
        public String owner_id;
        public String group_id;
        public String photo_maxSize_url;
        public int likeCount;
        public bool isLiked;
        public String access_key;
        public VkCommentsViewModel comments { get; set; }
        public VkImageViewModel(String id, String owner_id, String photo_maxSize_url, int likeCount, bool isLiked)
        {
            this.id = id;
            this.owner_id = owner_id;
            this.photo_maxSize_url = photo_maxSize_url;
            this.likeCount = likeCount;
            this.isLiked = isLiked;
        }
    }
    public class VkCommentsViewModel
    {
        public String owner_id { get; set; }
        public List<Post> comments { get; set; }
        public List<NFProfile> profiles { get; set; }
        public List<NFGroup> groups { get; set; }
        public VkCommentsViewModel()
        {
            comments = new List<Post>();
            profiles = new List<NFProfile>();
            groups = new List<NFGroup>();
        }
        public IProfile getProfileById(string id)
        {
            long id_ = Int64.Parse(id);
            if (id_ > 0)
            {
                foreach (NFProfile profile in profiles)
                {
                    if (profile.id.Equals(id))
                        return profile;
                }
            }
            else
            {
                id_ *= -1;
                foreach (NFGroup group in groups)
                {
                    if (group.id.Equals(id_.ToString()))
                        return group;
                }
            }
            return null;
        }
    }
}
