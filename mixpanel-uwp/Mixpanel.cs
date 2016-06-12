namespace mixpanel_uwp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Data.Json;
    using Windows.Web.Http;
    using Windows.Web.Http.Filters;

    namespace Mixpanel_uwp
    {
        public class Mixpanel
        {
            private const string MIXPANEL_TRACKING_URL = "http://api.mixpanel.com/track/?verbose=1&data=";
            private const string MIXPANEL_PROFILE_UPDATE_URL = "http://api.mixpanel.com/engage/?verbose=1&data=";
            private const string TR_TOKEN_PROFILE = "$token";
            private const string TR_TOKEN_EVENT = "token";

            private const string TR_DISTINCT_ID_PROFILE = "$distinct_id";
            private const string TR_DISTINCT_ID_EVENT = "distinct_id";

            private const string TR_WINDOWS_SESSIONS = "Windows sessions";
            private const string TR_EVENT = "event";
            private const string TR_PROPERTIES = "properties";

            private string Token;
            private HttpClient client;

            public Mixpanel(string _token)
            {
                Token = _token;

                var baseProtocolFilter = new HttpBaseProtocolFilter();
                baseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
                baseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
                client = new HttpClient(baseProtocolFilter);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="distinct_id"></param>
            /// <param name="setParameters"></param>
            /// <returns>True if the track call is successful, and a False otherwise</returns>
            public async Task<bool> updateProfile(string distinct_id, Dictionary<string, string> setParameters)
            {
                JsonObject setParametersJsonObj = DictionaryToJsonObject(setParameters);
                JsonObject jsonObject = new JsonObject();
                jsonObject.Add(TR_TOKEN_PROFILE, JsonValue.CreateStringValue(Token));
                jsonObject.Add(TR_DISTINCT_ID_PROFILE, JsonValue.CreateStringValue(distinct_id));
                jsonObject.Add("$set", setParametersJsonObj);

                string base64string = toBase64JsonObject(jsonObject);

                Uri url = new Uri(MIXPANEL_PROFILE_UPDATE_URL + base64string);
                return await sendEventOrUpdate(url);
            }

            /// <summary>
            /// Increments the windows sessions value on MixPanel
            /// </summary>
            /// <param name="distinct_id"></param>
            /// <returns>True if the track call is successful, and a False otherwise</returns>
            public async Task<bool> incrementWindowsSessions(string distinct_id)
            {
                JsonObject jsonObject = new JsonObject();
                jsonObject.Add(TR_TOKEN_PROFILE, JsonValue.CreateStringValue(Token));
                jsonObject.Add(TR_DISTINCT_ID_PROFILE, JsonValue.CreateStringValue(distinct_id));

                // increment windows sessions
                JsonObject addObj = new JsonObject();
                addObj.Add(TR_WINDOWS_SESSIONS, JsonValue.CreateNumberValue(1));
                jsonObject.Add("$add", addObj);

                string base64string = toBase64JsonObject(jsonObject);

                Uri url = new Uri(MIXPANEL_PROFILE_UPDATE_URL + base64string);
                return await sendEventOrUpdate(url);
            }

            /// <summary>
            /// Tracks an event to MixPanel Tracker
            /// </summary>
            /// <param name="eventName">Event Name</param>
            /// <param name="distinct_id">Distinct ID</param>
            /// <param name="properties">Properties</param>
            /// <returns>True if the track call is successful, and a False otherwise</returns>
            public async Task<bool> trackEvent(string eventName, string distinct_id,
                Dictionary<string, string> properties)
            {
                JsonObject jsonObject = new JsonObject();

                jsonObject.Add(TR_EVENT, JsonValue.CreateStringValue(eventName));

                JsonObject propertiesObj = DictionaryToJsonObject(properties);
                propertiesObj.Add(TR_TOKEN_EVENT, JsonValue.CreateStringValue(Token));
                propertiesObj.Add(TR_DISTINCT_ID_EVENT, JsonValue.CreateStringValue(distinct_id));
                jsonObject.Add(TR_PROPERTIES, propertiesObj);

                string base64string = toBase64JsonObject(jsonObject);

                Uri url = new Uri(MIXPANEL_TRACKING_URL + base64string);
                return await sendEventOrUpdate(url);
            }

            /// <summary>
            /// Sends the get request to MixPanel
            /// </summary>
            /// <param name="url">The URL to the MixPanel request</param>
            /// <returns>True if the track response is "1", and a False if "0"</returns>
            private async Task<bool> sendEventOrUpdate(Uri url)
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.Content.ToString().Contains("\"status\": 1"))
                    return true;
                else
                {
                    Debug.WriteLine("MIXPANEL track failed, response: " + response.Content.ToString());
                    return false;
                }
            }

            /// <summary>
            /// Encodes the json object of the parameters to Base64
            /// </summary>
            /// <param name="parameters">ictionary with the tracking parameters</param>
            /// <returns></returns>
            private JsonObject DictionaryToJsonObject(Dictionary<String, String> parameters)
            {
                JsonObject paramsObject = new JsonObject();
                foreach (KeyValuePair<string, string> entry in parameters)
                {
                    if (!String.IsNullOrEmpty(entry.Value))
                        paramsObject.Add(entry.Key, JsonValue.CreateStringValue(entry.Value));
                }
                return paramsObject;
            }

            /// <summary>
            /// Encodes the json object with the track information to Base64
            /// </summary>
            /// <param name="obj">Json Object</param>
            /// <returns>Base64 string</returns>
            private string toBase64JsonObject(JsonObject obj)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(obj.ToString()));
            }
        }
    }

}
