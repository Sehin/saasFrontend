using MvcWebRole1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class FacebookController : Controller
    {
        //
        // GET: /Facebook/
        DatabaseContext db = new DatabaseContext();

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        String client_id = "1476076429349792";
        String client_secret = "4d078d1a26961a9c81dd71bc87831b35";
        String redirect_uri = "http://localhost:65413/Facebook/getToken/";
        String[] scopeParams;
        public RedirectResult Auth()
        {
            #region scopeParams
            scopeParams = new String[2];
            scopeParams[1] = "user_managed_groups";
            scopeParams[0] = "publish_actions";
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
            return new RedirectResult("https://www.facebook.com/dialog/oauth?client_id=" + client_id + "&scope=" + scopeParamsString + "&redirect_uri=" + redirect_uri);
        }

        public RedirectResult getToken(String code)
        {
            Console.WriteLine("getTokenMethod");
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String access_token = wc.DownloadString("https://graph.facebook.com/oauth/access_token?client_id=" + client_id + "&client_secret=" + client_secret + "&code=" + code + "&redirect_uri=" + redirect_uri);
            
 
            
                string login = HttpContext.User.Identity.Name;
                User user = db.Users.Where(u=>u.Email==login).FirstOrDefault();
                //SocAccount socAcc = db.SocAccounts.Where(u => u.ID_USER == user.Id).FirstOrDefault();
                SocAccount socAcc = new SocAccount();
                socAcc.ID_USER = user.Id;
                socAcc.SOCNET_TYPE = 0;
                socAcc.TOKEN = (access_token.Split('='))[1].Split('&')[0];
                db.SocAccounts.Add(socAcc);
                db.SaveChanges();
            return RedirectPermanent("~/Tool/AccountManage/");
        }

        public bool isHaveValidToken(int socId)
        {
            String message;
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            try
            {
                message = wc.DownloadString("https://graph.facebook.com/me?access_token=" + getFBToken(socId));
            }
            catch (System.Net.WebException)
            {
                return false;
            }
            return true;
        }

        public String getFBToken(int socId)
        {
            string login = HttpContext.User.Identity.Name;
            User user = db.Users.Where(u => u.Email == login).FirstOrDefault();
            SocAccount socAcc = db.SocAccounts.Where(u => u.ID_USER == user.Id).Where(u => u.ID_AC == socId).FirstOrDefault();
            if (socAcc == null)
                return null;
            else
                return socAcc.TOKEN;
        }

        public String getUserName(int socId)
        {
            try
            {
                String name;
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                name = wc.DownloadString("https://graph.facebook.com/me?access_token=" + getFBToken(socId));
                JObject test = JObject.Parse(name);

                return (string)test["name"];
            }
            catch (WebException)
            {
                return "Неправильный accessToken";
            }
        }

        public String getGroupName(string group_id, int socId)
        {
            try
            {
                String name;
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                name = wc.DownloadString("https://graph.facebook.com/"+group_id+"?access_token=" + getFBToken(socId));
                JObject test = JObject.Parse(name);

                return (string)test["name"];
            }
            catch (WebException)
            {
                return "Error";
            }
        }
        public String getUserURL(int socId)
        {
            try
            {
                String url;
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                url = wc.DownloadString("https://graph.facebook.com/me?access_token=" + getFBToken(socId));
                JObject test = JObject.Parse(url);

                return (string)test["link"];
            }
            catch (WebException)
            {
                return "#";
            }
        }

        public string createPost(String message, String groupId, int socId)
        {
                var pars = new NameValueCollection();
                String url = "https://graph.facebook.com/v2.3/871937059545740/feed/";
                pars.Add("message", message);
                pars.Add("access_token", getFBToken(socId));
                JObject answer = POST(url, pars);
                return (string)answer["id"];
        }

        public int getPostLikes(String postId, int socId)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String url = "https://graph.facebook.com/"+postId+"/likes?total_count&summary=true&access_token=" + getFBToken(socId);
            url = wc.DownloadString(url);
            JObject obj = JObject.Parse(url);
            int count = (int)obj["summary"]["total_count"];
            return count;
        }

        public int getGroupLikes(String groupId, int socId)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String url = "https://graph.facebook.com/" + groupId + "/feed?summary=true&access_token=" + getFBToken(socId);
            url = wc.DownloadString(url);
            JObject obj = JObject.Parse(url);
            JToken jtoken = obj["data"].First;
            int count = 0;
            do
            {
                JToken likes = jtoken["likes"]["data"];
                likes = likes.First;
                do
                {
                    count++; likes = likes.Next;
                }
                while (likes != null);


                jtoken = jtoken.Next;
            }
            while (jtoken != null);
            return count;
        }

        public int getGroupRepost(String groupId, int socId)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String url = "https://graph.facebook.com/" + groupId + "/feed?summary=true&access_token=" + getFBToken(socId);
            url = wc.DownloadString(url);
            JObject obj = JObject.Parse(url);
            JToken jtoken = obj["data"].First;
            int count = 0;
            do
            {
                if (jtoken["shares"]!=null)
                    count += (int)jtoken["shares"]["count"];
                


                jtoken = jtoken.Next;
            }
            while (jtoken != null);
            return count;
        }

        public int getPostRepost(String postId, int socId)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String url = "https://graph.facebook.com/" + postId + "?access_token=" + getFBToken(socId);
            url = wc.DownloadString(url);
            JObject obj = JObject.Parse(url);
            int count = 0;
            try
            {
                 count = (int)obj["shares"]["count"];
            }
            catch (NullReferenceException)
            {
                return count;
            }
            return count;
        }
        
        public ArrayList getPostLikesId(String postId, int socId)
        {
            ArrayList ids = new ArrayList();
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String url = "https://graph.facebook.com/" + postId + "/likes?total_count&access_token=" + getFBToken(socId);
            url = wc.DownloadString(url);
            JObject obj = JObject.Parse(url);
            JToken id = obj["data"].First;

            do
            {

                ids.Add((string)id["id"]);
                id = id.Next;
            } while (id != null);
            return ids;
        }

        public ArrayList getGroupLikesId(String groupId, int socId)
        {
            ArrayList ids = new ArrayList();
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String url = "https://graph.facebook.com/" + groupId + "/feed?summary=true&access_token=" + getFBToken(socId);
            url = wc.DownloadString(url);
            JObject obj = JObject.Parse(url);
            JToken token = obj["data"].First;

            do
            {
                JToken likes = token["likes"]["data"];
                likes = likes.First;
                do{
                    bool isHave = false;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        if (ids[i].Equals((string)likes["id"]))     // Если уже есть такой id - то не добавляем
                            isHave = true;
                    }
                    if (!isHave)
                        ids.Add((string)likes["id"]);
                    likes=likes.Next;
                }while(likes!=null);
                token = token.Next;
            } while (token != null);

                return ids;
        }

        public JObject POST(String url, NameValueCollection pars)
        {
            WebClient webClient = new WebClient();
            var response = webClient.UploadValues(url, pars);
            JObject answer = JObject.Parse(System.Text.Encoding.Default.GetString(response));
            return answer;
        }

        public String getGroupInfo(String groupId, int socId)
        {
            String groupName = getGroupName(groupId, socId);
            String g_url = "https://www.facebook.com/groups/" + groupId;
            return "<a href=\""+g_url+"\">"+groupName+"</a>";
        }
    }
}

    