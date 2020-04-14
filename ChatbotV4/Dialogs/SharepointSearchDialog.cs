using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SharePoint.Client;

namespace CoreBot.Dialogs
{
    public class SharepointSearchDialog
    {

        List<string> OPATH = new List<string>();
        List<string> finalPathList = new List<string>();

        int KEYFOUND = 0;
        int patternMatch = 0;
        List<string> res = new List<string>();

    



        List<string> getChildNodes(XmlNode cn)
        {


            foreach (XmlNode xN in cn.ChildNodes)
            {


                if ((cn.LocalName.Equals("Key") && cn.InnerText.Equals("OriginalPath")) || (cn.LocalName.Equals("Key") && cn.InnerText.Equals("Path")))
                {
                    var next = cn.NextSibling;
                    OPATH.Add(next.InnerText);
                    KEYFOUND = 1;
                    return OPATH;
                }

                getChildNodes(xN);
            }


            return OPATH;
        }

        //public async Task writeFile(string text)
        //{

        //    string sharePointXmlPath1 = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory.ToString(), "Sharepoint_Gaurav.xml");

        //    System.IO.File.WriteAllText(sharePointXmlPath1, text);
        //}
        public  async Task<List<string>> SharepointSearchEng(string searchstr,string docType)
        {
            try
            {

    

                string userName = "svc.data_analytics@MurphyOilCorp.com";
                string password = "PennyRoyal8696!";
                //string[] searchString = searchstr.Split(',');
                //string docType = searchString[1];
                //docType = docType.Replace(" ", String.Empty);
                //string searchStrFinal = searchString[0].Replace("&", "And");
                //string searchWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(searchstr);
                string apiUrl = "https://murphyoil.sharepoint.com/_api/search/query?querytext=" + "\'" + searchstr + "\'";
                var securePassword = new SecureString();
                foreach (char c in password.ToCharArray()) securePassword.AppendChar(c);
                var credential = new SharePointOnlineCredentials(userName, securePassword);
                Uri uri = new Uri(apiUrl);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "GET";
                request.Credentials = credential;
                request.Headers[HttpRequestHeader.Cookie] = credential.GetAuthenticationCookie(new Uri(apiUrl), true);  // SPO requires cookie authentication
                request.Headers["X-FORMS_BASED_AUTH_ACCEPTED"] = "f";

                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                Stream webStream = webResponse.GetResponseStream();
                StreamReader responseReader = new StreamReader(webStream);
                string response = responseReader.ReadToEnd();
                string xmlFilePath = Directory.GetCurrentDirectory();
                string xmlFile = xmlFilePath + "SharePoint.xml";

                string sharePointXmlPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory.ToString(), "Sharepoint.xml");

                System.IO.File.WriteAllText(sharePointXmlPath, response);
                Console.WriteLine(response);
                //  Console.WriteLine(response);
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(sharePointXmlPath);
                //  XmlNodeList str = xDoc.GetElementsByTagName("d:Cells");

                //  XmlNodeList str = xDoc.SelectNodes($"//d:Key[.='indexSystem']");
                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {

                    if (KEYFOUND == 1)
                    {
                        break;
                    }
                    getChildNodes(node);

                }

                Console.WriteLine(OPATH);
                Console.WriteLine(KEYFOUND);
                OPATH = OPATH.Distinct().ToList();

                if (KEYFOUND == 0)
                {

                    return OPATH;
                }

                //get the pattern from user and match

                if (searchstr.Equals("All", comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    patternMatch = 1;
                    return OPATH;
                }

                else
                {

                    foreach (var oPath in OPATH)
                    {
                        Match m = Regex.Match(oPath, docType, RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            Console.WriteLine("Found '{0}' at position {1}.", m.Value, m.Index);
                                finalPathList.Add(oPath);
                            patternMatch = 1;
                        }
                    }

                }

                if (patternMatch == 0)
                {
                    finalPathList.Add("Not Found");
                    return finalPathList;
                }
                else
                {
                    return finalPathList;
                }

            }

            catch
            {
                Console.WriteLine("Exceptions");
                finalPathList.Add("Not Found");
                return finalPathList;
            }
        }

    }
}
