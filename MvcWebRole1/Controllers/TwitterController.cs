using MvcWebRole1.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TweetSharp;

namespace MvcWebRole1.Controllers
{
    public class TwitterController : Controller
    {
        private string TwitterConsumerKey = ConfigurationManager.AppSettings["TwitterConsumerKey"];
        private string TwitterConsumerSecret = ConfigurationManager.AppSettings["TwitterConsumerSecret"];


        // GET: /Twitter/Index
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Auth()
        {
            // Step 1 - Retrieve an OAuth Request Token
            TwitterService service = new TwitterService(TwitterConsumerKey, TwitterConsumerSecret);

            // This is the registered callback URL
            OAuthRequestToken requestToken = service.GetRequestToken(ConfigurationManager.AppSettings["uri"] + "Twitter/AuthorizeCallback");
            // OAuthRequestToken requestToken = service.GetRequestToken("http://localhost:65413/Twitter/AuthorizeCallback");

            // Step 2 - Redirect to the OAuth Authorization URL
            Uri uri = service.GetAuthorizationUri(requestToken);
            return new RedirectResult(uri.ToString(), false /*permanent*/);
        }

        // This URL is registered as the application's callback at http://dev.twitter.com
        public ActionResult AuthorizeCallback(string oauth_token, string oauth_verifier)
        {
            var requestToken = new OAuthRequestToken { Token = oauth_token };

            // Step 3 - Exchange the Request Token for an Access Token
            TwitterService service = new TwitterService(TwitterConsumerKey, TwitterConsumerSecret);
            OAuthAccessToken accessToken = service.GetAccessToken(requestToken, oauth_verifier);
            // Step 4 - User authenticates using the Access Token
            service.AuthenticateWith(accessToken.Token, accessToken.TokenSecret);

            String token = accessToken.Token + " " + accessToken.TokenSecret;

            DatabaseContext db = new DatabaseContext();
            db.SocAccounts.Add(new SocAccount(2, token, getUserId()));
            db.SaveChanges();
            return View();
        }

        public int getUserId()
        {
            DatabaseContext db = new DatabaseContext();
            string login = HttpContext.User.Identity.Name;
            return db.Users.Where(u => u.Email == login).FirstOrDefault().Id;
        }
    }
}
