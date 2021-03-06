﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using PP.Signicat.WebApi.Models.CallBackHandlers;
using RazorEngine;

namespace PP.Signicat.WebApi.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class CallbackController : ApiController
    {
        // GET: api/Callback/GetSigning
        [ActionName("GetSigning")]
        [HttpGet]
        public async Task<HttpResponseMessage> Get(string orgname, string requestId, string taskId)
        {
            var statuscode = await new CallBackHandler().GetDocumentSigning(requestId, taskId, orgname);

            var response = new HttpResponseMessage();
            response.StatusCode = statuscode;
            return response;
        }

        // GET: api/Callback/GetLandingPage
        [ActionName("Landingpage")]
        [HttpGet]
        public HttpResponseMessage Get(int lcid)
        {
            var viewPath = HttpContext.Current.Server.MapPath(@Resources.Resourceeng.callbackpage);
            if (lcid == 1044)
                viewPath = HttpContext.Current.Server.MapPath(@Resources.Resourcenb.callbackpage);

            var template = File.ReadAllText(viewPath);
            var parsedView = Razor.Parse(template);

            var response = new HttpResponseMessage();
            response.Content = new StringContent(parsedView);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }

        // GET: api/Callback/DeactivateSigning
        [ActionName("DeactivateSigning")]
        [HttpPost]
        public async Task<HttpResponseMessage> Post(string orgname, string requestId, string taskId)
        {
            var statuscode = await new CallBackHandler().DeactivateTask(requestId, taskId, orgname);

            var response = new HttpResponseMessage();
            response.StatusCode = statuscode;
            return response;
        }
    }
}