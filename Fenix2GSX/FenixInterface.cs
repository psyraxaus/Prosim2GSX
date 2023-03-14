﻿using System;
using System.Threading;
using System.Globalization;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using CefSharp;
using CefSharp.OffScreen;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fenix2GSX
{
    public class FenixInterface
    {
        public static readonly string jsonURI = "http://localhost:8083/graphql";
        public static readonly string efbURI = "http://localhost:8083";

        private ChromiumWebBrowser Browser;
        private bool handlerRunning = false;
        private bool handlerExecuted = false;
        private HttpClient httpClient;

        public FenixInterface()
        {
            httpClient = new HttpClient();
        }

        public static string MsgMutation(string writeType, string name, object value)
        {
            if (writeType == "bool")
            {
                return string.Format("{{\"query\": \"mutation ($variableName: String!) {{dataRef {{ writeBool(name: $variableName, value: {0}) }} }}\", \"variables\": {{ \"variableName\": \"{1}\" }} }}", ((bool)value).ToString().ToLowerInvariant(), name);
            }
            else if (writeType == "float")
            {
                return string.Format(CultureInfo.InvariantCulture.NumberFormat, "{{\"query\": \"mutation ($variableName: String!, $variableValue: Float!) {{dataRef {{ writeFloat(name: $variableName, value: $variableValue) }} }}\", \"variables\": {{ \"variableName\": \"{0}\", \"variableValue\": {1:F8} }} }}", name, (float)value);
            }
            else if (writeType == "string")
            {
                return string.Format("{{\"query\": \"mutation ($variableName: String!, $variableValue: String!) {{dataRef {{ writeString(name: $variableName, value: $variableValue) }} }}\", \"variables\": {{ \"variableName\": \"{0}\", \"variableValue\": \"{1}\" }} }}", name, value);
            }
            else
                return "";
        }

        public static string MsgQuery(string dataRef, string id)
        {
            return string.Format("{{\"query\":\"query {{ dataRef {{{0}: dataRef(name: \\\"{1}\\\") {{value}} }} }}\",\"variables\":{{}} }}", id, dataRef);
        }

        public async void FenixPost(string msg)
        {
            var post = new HttpRequestMessage(HttpMethod.Post, jsonURI);
            post.Content = new StringContent(msg, Encoding.UTF8, "application/json");
            var response = httpClient.Send(post);
        }

        public string FenixGet(string msg)
        {
            var post = new HttpRequestMessage(HttpMethod.Post, jsonURI);
            post.Content = new StringContent(msg, Encoding.UTF8, "application/json");
            var response = httpClient.Send(post);

            return response.Content.ReadAsStringAsync().Result;
        }

        public string FenixGetVariable(string name)
        {
            var post = new HttpRequestMessage(HttpMethod.Post, jsonURI);
            post.Content = new StringContent(MsgQuery(name, "queryResult"), Encoding.UTF8, "application/json");
            var response = httpClient.Send(post);

            string result = response.Content.ReadAsStringAsync().Result;
            JObject dataRef = JObject.Parse(result);
            return dataRef["data"]["dataRef"]["queryResult"]["value"].ToString();
        }

        public void TriggerFinalOnEFB()
        {
            Browser = new ChromiumWebBrowser(efbURI);
            Browser.LoadingStateChanged += EfbHandler;

            while (!handlerExecuted) { }
            handlerRunning = false;
            handlerExecuted = false;
            Browser.Dispose();
            Browser = null;
        }

        private async void EfbHandler(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading && !handlerRunning)
            {
                handlerRunning = true;
                Browser.ExecuteScriptAsync("prepCloseLS();");
                await Task.Delay(2500);

                Browser.ExecuteScriptAsync("openApp('fenix','no','1');");
                await Task.Delay(4000);

                var foo = Browser.GetBrowser().GetFrameIdentifiers();
                foreach (var bar in foo)
                    Browser.GetBrowser().GetFrame(bar).ExecuteJavaScriptAsync("importSB(true);");
                await Task.Delay(7500);

                foreach (var bar in foo)
                    Browser.GetBrowser().GetFrame(bar).ExecuteJavaScriptAsync("boardingStatus = 'ended'; cargoStatus = 'ended'; fuelStatus = 'ended'; notifShown = true; generateFinalLoadsheet();");
                await Task.Delay(5000);
                foreach (var bar in foo)
                    Browser.GetBrowser().GetFrame(bar).ExecuteJavaScriptAsync("resendLS('final');");
                await Task.Delay(5000);
                handlerExecuted = true;
            }
        }
    }
}
