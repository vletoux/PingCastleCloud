﻿//
// Copyright (c) Vincent LE TOUX for Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//
using PingCastleCloud.Common;
using PingCastleCloud.Credentials;
using PingCastleCloud.RESTServices;
using PingCastleCloud.RESTServices.Azure;
using PingCastleCloud.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace PingCastleCloud.Tokens
{
    public class TokenFactory
    {
        public static async Task<Token> GetToken<T>(IAzureCredential credential) where T : IAzureService
        {
            if (credential is PRTCredential)
            {
                Trace.WriteLine("GetToken with PRT");
                var prt = await GetPRT(credential);
                var response1 = await RunAuthorize<T>(credential, prt);
                var code = ExtractCodeFromResponse(response1);
                var service = AzureServiceAttribute.GetAzureServiceAttribute<T>();
                var token = await RunGetToken<T>(credential, code, service.RedirectUri);

                return Token.LoadFromString(token);
            }
            if (credential is CertificateCredential)
            {
                Trace.WriteLine("GetToken with Certificate");
                var certCred = (CertificateCredential)credential;
                var token = await GetTokenWithCertAsync<T>(certCred);
                return token;
            }
            Trace.WriteLine("GetToken with dialog");
            return AuthenticationDialog.Authenticate<T>(credential);
        }

        public static async Task<Token> RefreshToken<T>(string TenantId, Token token) where T : IAzureService
        {
            Trace.WriteLine("Called RefreshToken");
            var service = AzureServiceAttribute.GetAzureServiceAttribute<T>();
            var httpClient = HttpClientHelper.GetHttpClient();
            var endpoint = EndPointAttribute.GetEndPointAttribute<T>();
            using (var response = await httpClient.PostAsync(endpoint.AuthorizeEndPoint.Replace("common", TenantId),
                new FormUrlEncodedContent(
                    new Dictionary<string, string>()
                    {
                        { "resource", service.Resource },
                        { "client_id", service.ClientID.ToString() },
                        {"grant_type", "refresh_token"},
                        {"refresh_token", token.refresh_token},
                        { "scope","openid"},
                    })))
            {
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                return Token.LoadFromString(responseString);
            }
        }

        public static long ToEpochTime(DateTime date)
        {
            return new DateTimeOffset(date).ToUnixTimeSeconds();
        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static async Task<Token> GetTokenWithCertAsync<T>(CertificateCredential credential) where T : IAzureService
        {
            var service = AzureServiceAttribute.GetAzureServiceAttribute<T>();
            var httpClient = HttpClientHelper.GetHttpClient();
            var input = new Dictionary<string, string>()
                    {
                        { "client_id", credential.ClientId },
                        {"scope", service.Resource + (service.Resource.EndsWith("/") ? null : "/") + ".default"},
                        {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"},
                        { "client_assertion", BuildJwtAssertion<T>(credential) },
                        { "grant_type", "client_credentials"},
                    };

            var endpoint = EndPointAttribute.GetEndPointAttribute<T>();

            using (var response = await httpClient.PostAsync(endpoint.TokenEndPoint.Replace("common", credential.Tenantid),
                new FormUrlEncodedContent(
                    input)))
            {
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync();
                return Token.LoadFromString(responseString);
            }
        }

        //https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow#second-case-access-token-request-with-a-certificate
        private static string BuildJwtAssertion<T>(CertificateCredential credential) where T : IAzureService
        {

            var header = new JwtHeader()
            {
                alg = "RS256",
                typ = "JWT",
                x5t = Convert.ToBase64String(StringToByteArray(credential.ThumbPrint)),
            };
            var endpoint = EndPointAttribute.GetEndPointAttribute<T>();

            var payload = new JwtPayload()
            {
                aud = endpoint.TokenEndPoint.Replace("common", credential.Tenantid),
                exp = ToEpochTime(DateTime.UtcNow.AddHours(1)),
                iss = credential.ClientId,
                jti = Guid.NewGuid().ToString(),
                nbf = ToEpochTime(DateTime.UtcNow.AddHours(-1)),
                sub = credential.ClientId,
                iat = ToEpochTime(DateTime.UtcNow),
            };
            string rawHeader = header.ToBase64JsonString();
            string rawPayload = payload.ToBase64JsonString();
            byte[] toSign = Encoding.UTF8.GetBytes(rawHeader + "." + rawPayload);
            // RSASSA-PKCS1-v1_5 with the SHA-256 hash algorithm
            byte[] signature = credential.PrivateKey.SignData(toSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return rawHeader + "." + rawPayload + "." + Convert.ToBase64String(signature);

        }

        private static string ExtractCodeFromResponse(string response1)
        {
            Trace.WriteLine("ExtractCodeFromResponse");
            string code = null;
            XmlDocument xmlDoc = new XmlDocument();

            using (var ms = new MemoryStream())
            {
                var bytes = Encoding.UTF8.GetBytes(response1);
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                xmlDoc.Load(ms);
            }

            XmlNode titleNode = xmlDoc.SelectSingleNode("//html/body/script");

            if (titleNode != null)
            {
                Trace.WriteLine("TitleNode found");
                code = titleNode.InnerText.Split('?')[1].Split('\\')[0].Split('=')[1];
            }
            else
            {
                Trace.WriteLine("TitleNode not found");
                var hrefNode = xmlDoc.SelectSingleNode("//html/body/h2/a/@href");
                if (hrefNode != null)
                {
                    Trace.WriteLine("A href found");
                    var link = hrefNode.InnerText;
                    var builder = new UriBuilder(link);
                    var query = HttpUtility.ParseQueryString(builder.Query);
                    if (!string.IsNullOrEmpty(query["code"]))
                    {
                        Trace.WriteLine("code found");
                        return query["code"];
                    }
                    if (query["sso_nonce"] != null)
                    {
                        Trace.WriteLine("sso_nonce found");
                        var sso_nonce = query["sso_nonce"];
                        throw new ApplicationException("SSO_Nonce " + sso_nonce);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return code;
        }

        private static async Task<string> GetPRT(IAzureCredential credential)
        {
            var httpClient = HttpClientHelper.GetHttpClient();

            ChallengeResponse cr;
            using (var response = await httpClient.PostAsync(Constants.OAuth2TokenEndPoint,
                new FormUrlEncodedContent(
                    new Dictionary<string, string>()
                    {
                        {"grant_type", "srv_challenge"},
                    })))
            {
                var r = await response.Content.ReadAsStringAsync();
                cr = ChallengeResponse.LoadFromString(r);
            }
            string aep = Constants.OAuth2AuthorizeEndPoint;
            if (!string.IsNullOrEmpty(credential.TenantidToQuery))
            {
                aep = aep.Replace("common", credential.TenantidToQuery);
            }
            string uri = HttpClientHelper.BuildUri(aep,
                new Dictionary<string, string> {
                    { "sso_nonce", cr.Nonce } ,
                });
            var o = CookieInfoManager.GetCookieInforForUri(uri);
            var token = o[0].Data.Split(';')[0];
            return token;
        }

        public static List<JwtToken> GetRegisteredPRTIdentities()
        {
            var output = new List<JwtToken>();
            Trace.WriteLine("GetRegisteredPRTIdentities");
            var o = CookieInfoManager.GetCookieInforForUri(Constants.OAuth2TokenEndPoint);
            if (o != null)
            {
                Trace.WriteLine(o.Count + " identities");
                foreach (var i in o)
                {
                    Trace.WriteLine("Identity: " + i.Data);
                    var prtToken = i.Data.Split(';')[0];
                    var sections = prtToken.Split('.');
                    if (sections.Length < 2)
                        continue;
                    var payload = sections[1];
                    Trace.WriteLine("Before loading token");
                    JwtToken t = JwtToken.LoadFromBase64String(payload);
                    Trace.WriteLine("Token: " + t.unique_name);
                    output.Add(t);
                }
            }
            else
            {
                Trace.WriteLine("No identity");
            }
            return output;
        }

        private static async Task<string> RunAuthorize<T>(IAzureCredential credential, string prtToken) where T : IAzureService
        {
            var sections = prtToken.Split('.');
            var payload = sections[1];

            JwtToken t = JwtToken.LoadFromBase64String(payload);

            var mscrid = Guid.NewGuid();
            var requestId = mscrid;

            var service = AzureServiceAttribute.GetAzureServiceAttribute<T>();

            var aep = Constants.OAuth2AuthorizeEndPoint;
            if (!string.IsNullOrEmpty(credential.TenantidToQuery))
            {
                aep = aep.Replace("common", credential.TenantidToQuery);
            }

            Trace.WriteLine("RunAuthorize: post to " + aep);

            string uri = HttpClientHelper.BuildUri(aep,
                new Dictionary<string, string> {
                    { "resource", service.Resource } ,
                    { "client_id", service.ClientID.ToString()},
                    { "response_type", "code" },
                    { "redirect_uri", service.RedirectUri},
                    { "client-request-id", requestId.ToString()},
                    { "mscrid", mscrid.ToString()},
                    { "sso_nonce", t.request_nonce}
                });

            var httpClient = HttpClientHelper.GetHttpClient();

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Add("x-ms-RefreshTokenCredential", prtToken);
                var response = await httpClient.SendAsync(request);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> RunGetToken<T>(IAzureCredential credential, string code, string redirectUri, string code_verifier = null) where T : IAzureService
        {
            var service = AzureServiceAttribute.GetAzureServiceAttribute<T>();
            var httpClient = HttpClientHelper.GetHttpClient();
            var input = new Dictionary<string, string>()
                    {
                        { "client_id", service.ClientID.ToString() },
                        {"grant_type", "authorization_code"},
                        {"code", code},
                        { "redirect_uri", redirectUri},
                    };
            if (!string.IsNullOrEmpty(code_verifier))
            {
                input.Add("code_verifier", code_verifier);
            }

            var endpoint = EndPointAttribute.GetEndPointAttribute<T>();
            var tep = endpoint.TokenEndPoint;
            if (!string.IsNullOrEmpty(credential.TenantidToQuery))
            {
                tep = tep.Replace("common", credential.TenantidToQuery);
            }
            Trace.WriteLine("RunGetToken: post to " + tep);
            using (var response = await httpClient.PostAsync(tep,
                new FormUrlEncodedContent(
                    input)))
            {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
