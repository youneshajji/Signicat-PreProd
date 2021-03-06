﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using PP.Signicat.WebApi.SignicatPreProd;

namespace PP.Signicat.WebApi.Models.SignicatHandlers
{
    public class WordSignRequest
    {
        public async Task<sdsdocument> UploadDocument(HttpPostedFile postedFile, HttpContent postedContent)
        {
            var httpClientHandler = new HttpClientHandler { Credentials = new NetworkCredential("prosesspilotene", "Bond007") };
            using (var client = new HttpClient(httpClientHandler))
            {
                var helpers = new Helpers();
                var stream = postedFile.InputStream;
                var fileBytes = helpers.ReadToEnd(stream);
                HttpContent content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                HttpResponseMessage response = await client.PostAsync("https://preprod.signicat.com/doc/prosesspilotene/sds", content);
                string documentId = await response.Content.ReadAsStringAsync();

                var docName = postedContent.Headers.ContentDisposition.FileName.Split('.');

                return new sdsdocument
                {
                    id = docName[0],
                    refsdsid = documentId,
                    description = docName[0]
                };
            }
        }

        public string CreateSignRequest(sdsdocument uploadedDocument, string customerorg, string language, int method)
        {
            var lang = "en";
            if (language == "1044")
                lang = "nb";

            var signmethodText = new SignatureHandler().GetMethod(method);
            var signInfo = new SigningInfo();
            signInfo.signingMetodText = signmethodText;
            signInfo.isInk = true;

            var authMethod = new SignatureHandler().GetAuthSignatures(signInfo);
            var signatures = new SignatureHandler().GetSignatures(signInfo);
            bool bundle = signInfo.signingMetodText == "nbid";


            var request = new createrequestrequest
            {
                password = "Bond007",
                service = "prosesspilotene",
                request = new request[]
                {
                    new request
                    {
                        clientreference = "cliref1",
                        language = lang,
                        profile = "default",
                        document = new document[]
                        {
                            new sdsdocument
                            {
                                id = uploadedDocument.id,
                                refsdsid = uploadedDocument.refsdsid,
                                description = uploadedDocument.description
                            }
                        },
                        subject = new subject[]
                        {
                            new subject
                            {
                                id = "subj_1",
                                mobile = "99999999"
                                //nationalid = "1909740939"
                            }
                        },
                        task = new task[]
                        {
                            new task
                            {
                                id = "task_1",
                                subjectref = "subj_1",
                                bundle = true,
                                bundleSpecified = bundle,
                                documentaction = new documentaction[]
                                {
                                    new documentaction
                                    {
                                        type = documentactiontype.sign,
                                        documentref = uploadedDocument.id
                                    }
                                },
                                authenticationbasedsignature = authMethod,
                                signature = signatures
                            }
                        }
                    }
                }
            };

            for (int i = 0; i < request.request[0].task.Length; i++)
            {
                var callbackOnTaskCompleteUrl = "https://prosesspilotenesignicatwebapi-preprod.azurewebsites.net:443/api/Callback/Landingpage?lcid=" + Convert.ToString(language);
                var callbackNotificationUrl = "https://prosesspilotenesignicatwebapi-preprod.azurewebsites.net:443/api/Callback/GetSigning?orgname=" + customerorg;
                request.request[0].task[i].ontaskcomplete = callbackOnTaskCompleteUrl;

                request.request[0].task[i].notification = new[]
                {
                    new notification
                    {
                        notificationid = "req_callback_" + i,
                        type = notificationtype.URL,
                        recipient = callbackNotificationUrl,
                        message = "callbackurl",
                        schedule = new []
                        {
                            new schedule
                            {
                                stateis = taskstatus.completed
                            }
                        }
                    }
                };
            }

            createrequestresponse response;
            using (var client = new DocumentEndPointClient())
            {
                response = client.createRequest(request);
            }
            return
                String.Format("https://preprod.signicat.com/std/docaction/prosesspilotene?request_id={0}&task_id={1}",
                    response.requestid[0], request.request[0].task[0].id);
        }
    }
}