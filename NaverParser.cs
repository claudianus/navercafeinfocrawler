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
        private const string getCategoriesApi = "https://section.cafe.naver.com/SectionHome.nhn?t=1";
        private const string getSubCategoriesApi = "https://section.cafe.naver.com/SubDirectoryListAjax.nhn";
        private const string getCafeListApi = "https://section.cafe.naver.com/SectionHomeCafeListAjax.nhn";
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
            var matches = Regex.Matches(
                response.Replace(" id=\"theme1\"", ""),
                "<a href=\"#\" class=\"N=a:cnt_thm\\.dir1,i:(\\d+),t:1 _click\\(CategoryCafe\\|SelectNode\\|\\d+\\) _stopDefault\">(.+)</a>");

            foreach (Match match in matches)
            {
                var id = int.Parse(match.Groups[1].ToString());
                var name = match.Groups[2].ToString();

                categoryList.Add(new RootCategory(id, name));
            }

            return categoryList;
        }

        internal static List<SubCategory> GetSubCategories(int categoryId)
        {
            var subCategoryList = new List<SubCategory>();

            var response = Post(getSubCategoriesApi, $"pid={categoryId}");
            var json = JObject.Parse(response);

            var directoryArr = json["result"][0]["directoryList"];

            foreach (var directory in directoryArr)
            {
                var id = (int)directory["dirId"];
                var cafeCount = (int)directory["cafeCount"];
                var name = (string)directory["dirName"];
                var parentCategoryId = (int)directory["parentDirId"];

                subCategoryList.Add(new SubCategory(parentCategoryId, id, name, cafeCount));
            }

            return subCategoryList;
        }

        internal static List<Cafe> GetCafes(
            int maxFind,
            int minMember,
            int dir1Id,
            int dir2Id = 0,
            string listType = "AC",
            int sortType = 3,
            int dirType = 1)
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

                var postData =
                    $"search.dirType={dirType}&search.dir1Id={dir1Id}&search.dir2Id={dir2Id}&search.listType={listType}&search.sortType={sortType}&search.page={page}";

                string response;
                try
                {
                    response = Post(getCafeListApi, postData);
                } catch
                {
                    continue;
                }
                var json = JObject.Parse(response);

                var cafeArr = json["result"][0]["cafeList"];
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

                    var memberCount = (int)cafe["membercount"];

                    if (memberCount < minMember)
                    {
                        //더 이상 멤버수 조건에 충족하는 카페가 없을 경우 중지
                        breakWhile = true;
                        break;
                    }

                    var cafeId = (int)cafe["cafeId"];
                    var ownerName = (string)cafe["sysopnick"];
                    string ownerId;

                    try
                    {
                        ownerId = GetOwnerId(cafeId, ownerName);
                    }
                    catch
                    {
                       continue;
                    }
                    

                    if (cafeList.Any(x => x.ownerId == ownerId))
                    {
                        continue;
                    }

                    var cafeName = (string)cafe["clubname"].ToString().Replace("&quot;", "\"").Replace("&lt;","<").Replace("&gt", ">");
                    var cafeUrl = $"http://cafe.naver.com/{cafe["cluburl"]}";
                    var cafeNameEng = (string)cafe["cluburl"];
                    var cafeOpenDate = (string)cafe["opendate"];


                    cafeList.Add(new Cafe(cafeName, cafeNameEng, cafeUrl, cafeId, cafeOpenDate, memberCount, ownerName, ownerId));
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

        internal static string GetOwnerId(int cafeId, string ownerName)
        {
            var response = Get($"{getCafeInfoApi}?clubid={cafeId}", "euc-kr");
            var match = Regex.Match(
                response,
                $"ui\\(event, '(.+)',.+,'{ownerName}',.+,.+, .+, .+, .+, .+, .+\\)");
            return match.Groups[1].ToString();
        }
    }
}
