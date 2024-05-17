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
using SimpleJSON;

namespace MetaDota.DotaReplay
{
    

    public class WebMatchRequest
    {
        public int id;
        public string request;
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
            _webMatchRequest.id = int.Parse(node["id"]);
            _webMatchRequest.request = node["requestStr"];
            Console.WriteLine($"Get Match Request id = {_webMatchRequest.id} request = {_webMatchRequest.request}");
            return _webMatchRequest.request;
        }

        async Task SendResult()
        {
            Console.WriteLine($"Send Match Request Result = {_webMatchRequest.result} message = {_webMatchRequest.message}");
            string requestEndPoint = Program.config.webServerUrl + $"/GenerateOver?id={_webMatchRequest.id}&state={_webMatchRequest.result}&message={_webMatchRequest.message}";
            HttpRequestMessage request = this.CreateRequest(HttpMethod.Post, requestEndPoint);
            Task<HttpResponseMessage> response;
            try
            {
                response = _httpClient.SendAsync(request);
                HttpStatusCode resStatusCoode = response.Result.StatusCode;
            }
            catch (HttpRequestException e)
            {
                return ;
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





