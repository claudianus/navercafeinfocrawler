using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace 네이버카페정보수집
{
    internal static class NaverParser
    {
        private const string getCategoriesApi = "https://section.cafe.naver.com/cafe-home-api/v2/themecafes?page=1&perPage=0&sort=membercount&type=ac&themeDir1Id=2&themeDir2Id=0";
        private const string getSubCategoriesApi = "https://section.cafe.naver.com/cafe-home-api/v1/directories/{categoryId}/sub-directories";
        private const string getCafeListApi = "https://section.cafe.naver.com/cafe-home-api/v2/themecafes?page={pageId}&perPage=100&sort=membercount&type=ac&themeDir1Id={categoryId}&themeDir2Id={subCategoryId}";
        private const string getCafeInfoApi = "http://cafe.naver.com/CafeProfileView.nhn"; //?clubid=14252258

        private static string Post(string uri, string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Proxy = null;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Method = "POST";
            request.Timeout = 3000;

            using (var requestBody = request.GetRequestStream())
            {
                requestBody.Write(dataBytes, 0, dataBytes.Length);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }

        }

        private static string Get(string uri, string encoding = "utf-8")
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = 3000;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.GetEncoding(encoding)))
            {
                return reader.ReadToEnd();
            }
        }

        internal static List<RootCategory> GetRootCategories()
        {
            var categoryList = new List<RootCategory>();

            var response = Get(getCategoriesApi);
            var json = JObject.Parse(response);

            var directoryArr = json["message"]["result"]["themes"];

            foreach (var directory in directoryArr)
            {
                var id = (int)directory["themeId"];
                var name = (string)directory["themeName"];

                categoryList.Add(new RootCategory(id, name));
            }

            return categoryList;
        }

        internal static List<SubCategory> GetSubCategories(int categoryId)
        {
            var subCategoryList = new List<SubCategory>();

            var response = Get(getSubCategoriesApi.Replace("{categoryId}", categoryId.ToString()));
            var json = JObject.Parse(response);

            var directoryArr = json["message"]["result"]["directories"];

            foreach (var directory in directoryArr)
            {
                var id = (int)directory["directoryId"];
                var name = (string)directory["directoryName"];

                var response2 = Get(
                      getCafeListApi.Replace("{pageId}", "0")
                                    .Replace("{perPage}", "0")
                                    .Replace("{categoryId}", categoryId.ToString())
                                    .Replace("{subCategoryId}", id.ToString()));

                var json2 = JObject.Parse(response2);

                var cafeCount = (int)json2["message"]["result"]["pageInfo"]["totalCount"];

                subCategoryList.Add(new SubCategory(categoryId, id, name, cafeCount));
            }

            return subCategoryList;
        }

        internal static List<Cafe> GetCafes(
            int maxFind,
            int minMember,
            int categoryId,
            int subCategoryId = 0)
        {
            var cafeList = new List<Cafe>();

            var page = 1;
            var done = 0;

            Console.Clear();
            Console.WriteLine($"작업중... {done}/{maxFind} ({(float)done / maxFind * 100:F2}%)");

            while (true)
            {
                var breakWhile = false;

                if (done >= maxFind)
                {
                    //원하는 갯수만큼 수집한 경우 중지
                    break;
                }

                string response;
                try
                {
                    var apiUrl = getCafeListApi
                        .Replace("{pageId}", page.ToString())
                        .Replace("{categoryId}", categoryId.ToString())
                        .Replace("{subCategoryId}", subCategoryId.ToString());
                    Console.WriteLine(apiUrl);
                    response = Get(apiUrl);
                } catch
                {
                    continue;
                }
                var json = JObject.Parse(response);

                var cafeArr = json["message"]["result"]["cafes"];
                if (!cafeArr.Any())
                {
                    //페이지에 결과없으면 중지
                    break;
                }

                foreach (var cafe in cafeArr)
                {
                    if (done >= maxFind)
                    {
                        //원하는 갯수만큼 수집한 경우 중지
                        break;
                    }

                    var memberCount = (int)cafe["memberCount"];

                    if (memberCount < minMember)
                    {
                        //더 이상 멤버수 조건에 충족하는 카페가 없을 경우 중지
                        breakWhile = true;
                        break;
                    }

                    var cafeId = (int)cafe["cafeId"];
                    string ownerId;
                    string ownerName;

                    try
                    {
                        var cafeStaffInfo = GetStaffInfo(cafeId);
                        ownerId = cafeStaffInfo.managerId;
                        ownerName = cafeStaffInfo.managerName;
                    }
                    catch/* (Exception err)*/
                    {
                       //Console.WriteLine(err.ToString());
                       continue;
                    }


                    if (cafeList.Any(x => x.ownerId.Equals(ownerId)))
                    {
                        continue;
                    }

                    var cafeName = (string)cafe["cafeName"];
                    var cafeEngName = (string)cafe["cafeUrl"];
                    var cafeUrl = $"http://cafe.naver.com/{cafeEngName}";
                    var cafeNameEng = (string)cafe["cafeUrl"];
                    var cafeIntro = (string)cafe["introduction"];
                    var cafeCategoryId = (int)cafe["themeDir1Id"];
                    var cafeSubCategoryName = (string)cafe["themeDir2Name"];


                    cafeList.Add(new Cafe(cafeId, cafeName, cafeUrl, cafeIntro, cafeCategoryId, cafeSubCategoryName, memberCount, ownerId, ownerName, cafeEngName));
                    done++;

                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine($"작업중... {done}/{maxFind} ({(float)done / maxFind * 100:F2}%)");
                }

                if (breakWhile)
                {
                    break;
                }

                page++;
            }

            return cafeList;
        }

        internal static CafeStaff GetStaffInfo(int cafeId)
        {
            var response = Get($"{getCafeInfoApi}?clubid={cafeId}", "euc-kr");
            var match = Regex.Match(
                response,
                $"ui\\(event, '(.+)',.+,'(.+)','\\d+','', .+, .+, .+, .+, .+\\)");
            return new CafeStaff(match.Groups[2].Value, match.Groups[1].Value);
        }
    }
}
