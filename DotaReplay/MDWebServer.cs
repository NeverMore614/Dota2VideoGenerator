using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using ConsoleApp2;
using SteamKit2.CDN;
using MetaDota.config;
using static MetaDota.DotaReplay.MDReplayGenerator;
using SteamKit2;
using System.Net.Http;
using Aliyun.OSS;
using SimpleJSON;
using static Dota2.GC.Dota.Internal.CMsgClientProvideSurveyResult;

namespace MetaDota.DotaReplay
{
    

    public class WebMatchRequest
    {
        public string id;
        public string request;
        public string oldVideoUrl;
        public string result;
        public string message;
        public bool over;

        public void Invalid()
        {
            over = true;
            result = "fail";
            message = $"{ClientParams.WEB_INVALID_REQUEST}{request}";
        }
        public void Reset()
        { 
            over = false;
            request = "";
        }
    }
    internal class MDWebServer : SingleTon<MDWebServer>
    {
        private string requestFile = "webRequest.txt";

        private HttpClient _httpClient;

        private string _bearer;

        private WebMatchRequest _webMatchRequest;


        public MDWebServer()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            _httpClient = new HttpClient(clientHandler);

            _webMatchRequest = new WebMatchRequest();

        }

        public void Start()
        {
            _bearer = Program.config.GetBearer();
            if (string.IsNullOrEmpty(_bearer))
            {
                Console.WriteLine("no auth bearer, start fail");
                return;
            }

            Thread thread1 = new Thread(GetJob);
            thread1.Start();
        }

        async void GetJob()
        {
            //wait launch finish
            while (!Program.JobStart)
            {
                Thread.Sleep(100);
            }

            string matchRequest;
            while (true)
            {
                matchRequest = "";
                Thread.Sleep(10000);
                matchRequest = await GetMatchRequest();

                if (matchRequest == null) continue;

                if (!MDTools.CheckMatchValid(matchRequest))
                {
                    //match request string is invalid
                    _webMatchRequest.Invalid();
                    await SendResult();
                    continue;
                }
                Program.requestQueue.Enqueue(matchRequest);


                EReplayGenerateResult eReplayGenerateResult;
                //wait task over
                while (!_webMatchRequest.over)
                {
                    MDReplayGenerator.GetResult(matchRequest, _webMatchRequest);
                    Thread.Sleep(10000);
                }
                await SendResult();
            }
        }

        async Task<string> GetMatchRequest()
        {

            _webMatchRequest.Reset();
            if (File.Exists(requestFile))
            {
                string[] requests = File.ReadAllLines(requestFile);
                if (requests.Length >= 3)
                {
                    _webMatchRequest.id = requests[0];
                    _webMatchRequest.request = requests[1];
                    _webMatchRequest.oldVideoUrl = requests[2];
                    Console.WriteLine($"Get Match Request id = {_webMatchRequest.id} request = {_webMatchRequest.request}");
                    return _webMatchRequest.request;
                }
            }

            string requestEndPoint = Program.config.webServerUrl + "/GetMatchRequest";
            HttpRequestMessage request = this.CreateRequest(HttpMethod.Get, requestEndPoint);

            string resBodyStr;
            HttpStatusCode resStatusCoode = HttpStatusCode.NotFound;
            Task<HttpResponseMessage> response;
            try
            {
                response = _httpClient.SendAsync(request);
                resBodyStr = response.Result.Content.ReadAsStringAsync().Result;
                resStatusCoode = response.Result.StatusCode;
            }
            catch (HttpRequestException e)
            {
                // UNDONE: 通信失敗のエラー処理
                return null;
            }

            if (!resStatusCoode.Equals(HttpStatusCode.OK))
            {
                // UNDONE: レスポンスが200 OK以外の場合のエラー処理
                return null;
            }
            if (String.IsNullOrEmpty(resBodyStr))
            {
                // UNDONE: レスポンスのボディが空の場合のエラー処理
                return null;
            }
            JSONNode node = JSON.Parse(resBodyStr);
            _webMatchRequest.id = node["id"];
            _webMatchRequest.request = node["requestStr"];
            _webMatchRequest.oldVideoUrl = node["url"];
            if (File.Exists(requestFile)) {
                File.Delete(requestFile);
            }
            File.WriteAllLines(requestFile, new string[]{ _webMatchRequest.id,  _webMatchRequest.request, _webMatchRequest.oldVideoUrl });

            Console.WriteLine($"Get Match Request id = {_webMatchRequest.id} request = {_webMatchRequest.request}");
            return _webMatchRequest.request;
        }

        async Task SendResult()
        {
            if (_webMatchRequest.result.Equals("success"))
            {
                //upload aliyun oss
                UploadAliyunOos();
            }
            Console.WriteLine($"Send Match Request Result = {_webMatchRequest.result} message = {_webMatchRequest.message}");
            string requestEndPoint = Program.config.webServerUrl + $"/GenerateOver?id={_webMatchRequest.id}&state={_webMatchRequest.result}&message={_webMatchRequest.message}";
            HttpRequestMessage request = this.CreateRequest(HttpMethod.Post, requestEndPoint);
            Task<HttpResponseMessage> response;
            try
            {
                response = _httpClient.SendAsync(request);
                HttpStatusCode resStatusCoode = response.Result.StatusCode;
                if (resStatusCoode == HttpStatusCode.OK)
                {
                    if (File.Exists(requestFile))
                    {
                        File.Delete(requestFile);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                return ;
            }
        }

        private void UploadAliyunOos()
        {

            // yourEndpoint填写Bucket所在地域对应的Endpoint。以华东1（杭州）为例，Endpoint填写为https://oss-cn-hangzhou.aliyuncs.com。
            var endpoint = "https://oss-cn-shanghai.aliyuncs.com";
            // 从环境变量中获取访问凭证。运行本代码示例之前，请确保已设置环境变量OSS_ACCESS_KEY_ID和OSS_ACCESS_KEY_SECRET。
            var accessKeyId = Environment.GetEnvironmentVariable("OSS_ACCESS_KEY_ID");
            var accessKeySecret = Environment.GetEnvironmentVariable("OSS_ACCESS_KEY_SECRET");
            // 填写Bucket名称，例如examplebucket。
            var bucketName = "metadotares";
            // 填写Object完整路径，完整路径中不能包含Bucket名称，例如exampledir/exampleobject.txt。
            var objectName =  $"{_webMatchRequest.id}/{MDTools.RandomChar(6)}/video.mp4";
            // 填写本地文件的完整路径。如果未指定本地路径，则默认从示例程序所属项目对应本地路径中上传文件。
            var localFilename = Path.GetFullPath(_webMatchRequest.message);

            // 创建OssClient实例。
            var client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            //删除已存在的视频
            if (!string.IsNullOrEmpty(_webMatchRequest.oldVideoUrl))
            {
                try
                {
                    string deleteObjName = _webMatchRequest.oldVideoUrl.Substring(_webMatchRequest.oldVideoUrl.LastIndexOf("com/") + 4);
                    // 删除文件。
                    client.DeleteObject(bucketName, deleteObjName);
                    Console.WriteLine("Delete object succeeded");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Delete object failed. {0}", ex.Message);
                }
            }

            try
            {
                // 上传文件。
                var result = client.PutObject(bucketName, objectName, localFilename);
                Console.WriteLine("Put object succeeded");
                _webMatchRequest.message = $"https://metadotares.oss-cn-shanghai.aliyuncs.com/{objectName}";

            }
            catch (Exception ex)
            {
                _webMatchRequest.result = "fail";
                _webMatchRequest.message = "Upload File Fail" + ex.Message;
                Console.WriteLine("Put object failed, {0}", ex.Message);

            }
        }
        private HttpRequestMessage CreateRequest(HttpMethod httpMethod, string requestEndPoint)
        {
            var request = new HttpRequestMessage(httpMethod, requestEndPoint);
            return this.AddHeaders(request);
        }

        /// <summary>
        /// HTTPリクエストにヘッダーを追加する内部メソッドです。
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <returns>HttpRequestMessage</returns>
        private HttpRequestMessage AddHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", $"Bearer {Program.config.GetBearer()}");
            return request;
        }
    }


}





