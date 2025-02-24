﻿using System;
using System.Reflection;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serialization.Json;

namespace Unifonic
{
    /// <summary>
    /// Unifonic rest client
    /// </summary>
    public abstract class UnifonicClient
    {
        /// <summary>
        /// Base Url used in call all APIs
        /// </summary>
        public string BaseUrl { get; private set; }

        /// <summary>
        /// A character string that uniquely identifies your app, you will find your AppSid in "Dev Tools" after you login to Unifonic Digital Platform
        /// </summary>
        protected string AppSid { get; set; }
        /// <summary>
        /// Rest client
        /// </summary>
        protected RestClient Client;

        /// <summary>
        /// Initialize Rest client
        /// </summary>
        /// <param name="appSid"></param>
        /// <param name="baseUrl"></param>
        protected UnifonicClient(string appSid, string baseUrl)
        {
            BaseUrl = baseUrl;
            AppSid = appSid;

            var assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(assembly.FullName);
            var version = assemblyName.Version;

            Client = new RestClient
            {
                UserAgent = "unifonic-csharp/" + version + " (.NET " + Environment.Version + ")",
                BaseUrl = new Uri(BaseUrl),
                Timeout = 60000
            };
            Client.AddHandler("text/html", new JsonDeserializer());
            Client.AddDefaultParameter("AppSid", AppSid);
        }


        /// <summary>
        /// Execute a manual REST request
        /// </summary>
        /// <typeparam name="T">The type of object to create and populate with the returned data.</typeparam>
        /// <param name="request">The request to execute</param>
        public virtual T Execute<T>(IRestRequest request) where T : new()
        {
            request.OnBeforeDeserialization = resp =>
            {
                if (((int)resp.StatusCode) >= 400)
                {
                    //RestSharp doesn't like data[]
                    resp.Content = resp.Content.Replace(",\"data\":[]", string.Empty);
                }
            };

            var response = Client.Execute<BaseResult<T>>(request);
            if (response.Data != null && !response.Data.Success)
            {
                var unifonicException = new RestException(response.Data.ErrorCode, response.Data.Message);
                throw unifonicException;
            }
            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var unifonicException = new ApplicationException(message, response.ErrorException);
                throw unifonicException;
            }
            return response.Data != null ? response.Data.Data : default(T);
        }
    }
    public partial class UnifonicRestClient : UnifonicClient
    {
        /// <summary>
        /// Initializes a new client
        /// </summary>
        /// <param name="appSid">String that uniquely identifies your app, you will find your AppSid in "Dev Tools" after you login to Unifonic Digital Platform</param>
        public UnifonicRestClient(string appSid)
            : base(appSid, "http://api.unifonic.com/rest/")
        {
        }
        public UnifonicRestClient(string appSid,string baseUrl)
            : base(appSid, baseUrl)
        {
        }
    }
}
