//개발자 이메일: claudianus@engineer.com

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
// ReSharper disable SimplifyLinqExpression

namespace 네이버카페정보수집
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            Console.Title = "네이버 카페 정보 수집기";
            Console.WriteLine("카테고리 불러오는중...");
            var rootCategoryList = NaverParser.GetRootCategories();
            Console.Clear();
            int inputRootCategoryInt;
            while (true)
            {
                Console.WriteLine("카테고리 목록:");
                foreach (var rootCategory in rootCategoryList)
                {
                    Console.WriteLine($"{rootCategory.id}. {rootCategory.name}");
                }
                Console.WriteLine("\n카테고리를 입력해주세요. 예) 10");
                var inputRootCategory = Console.ReadLine();
                if (!int.TryParse(inputRootCategory, out inputRootCategoryInt))
                {
                    Console.WriteLine("숫자를 입력해야합니다.");
                    continue;
                }
                if (!rootCategoryList.Any(x => x.id == inputRootCategoryInt))
                {
                    Console.WriteLine("없는 카테고리입니다.");
                    continue;
                }
                break;
            }
            Console.Clear();
            Console.WriteLine("서브 카테고리 불러오는중...");
            var subCategoryList = NaverParser.GetSubCategories(inputRootCategoryInt);
            Console.Clear();
            int inputSubCategoryInt;
            while (true)
            {
                Console.WriteLine("서브 카테고리 목록:");
                Console.WriteLine("0. 전체");
                foreach (var subCategory in subCategoryList)
                {
                    Console.WriteLine($"{subCategory.id}. {subCategory.name} (카페수: {subCategory.cafeCount})");
                }
                Console.WriteLine("\n서브 카테고리를 입력해주세요. 예) 101");
                var inputSubCategory = Console.ReadLine();
                if (!int.TryParse(inputSubCategory, out inputSubCategoryInt))
                {
                    Console.WriteLine("숫자를 입력해야합니다.");
                    continue;
                }
                if (inputSubCategoryInt != 0 &&
                    !subCategoryList.Any(x => x.id == inputSubCategoryInt))
                {
                    Console.WriteLine("없는 서브 카테고리입니다.");
                    continue;
                }
                break;
            }
            Console.Clear();
            int inputMinMemberInt;
            while (true)
            {
                Console.WriteLine("수집할 카페의 최소 멤버 수를 입력해주세요. 예) 10000");
                var inputMinMember = Console.ReadLine();
                if (!int.TryParse(inputMinMember, out inputMinMemberInt))
                {
                    Console.WriteLine("숫자를 입력해야합니다.");
                    continue;
                }
                break;
            }
            Console.Clear();
            int inputMaxFindInt;
            while (true)
            {
                Console.WriteLine("최대 몇개를 찾으시겠습니까? 예) 100");
                var inputMaxFind = Console.ReadLine();
                if (!int.TryParse(inputMaxFind, out inputMaxFindInt))
                {
                    Console.WriteLine("숫자를 입력해야합니다.");
                    continue;
                }
                if (inputMaxFindInt < 1)
                {
                    Console.WriteLine("1 이상 입력해야합니다.");
                    continue;
                }
                break;
            }
            Console.Clear();
            Console.WriteLine("작업중...");
            //결과 구하고 출력
            var cafeList = NaverParser.GetCafes(inputMaxFindInt, inputMinMemberInt, inputRootCategoryInt, inputSubCategoryInt);
            Console.Clear();
            Console.WriteLine("결과: ");
            for (var i = 0; i < cafeList.Count; i++)
            {
                var cafe = cafeList[i];
                Console.WriteLine($"{i + 1}. {cafe.ownerName} ({cafe.ownerId})");
            }
            //결과 텍스트 파일로 저장
            var rootCategoryUsed = rootCategoryList.FirstOrDefault(x => x.id == inputRootCategoryInt).name;
            var subCategoryUsed = inputSubCategoryInt == 0 ?
                "전체" :
                subCategoryList.FirstOrDefault(x => x.id == inputSubCategoryInt).name;
            var sb = new StringBuilder();
            sb.AppendLine($"개발자 이메일: claudianus@engineer.com");
            sb.AppendLine($"작업완료시각: {DateTime.Now.ToString()}");
            sb.AppendLine($"[{rootCategoryUsed} > {subCategoryUsed}] 카테고리에 속하며");
            sb.AppendLine($"멤버 수 [{inputMinMemberInt}]명 이상인 카페들에서");
            sb.AppendLine($"최대 [{inputMaxFindInt}]개의 카페 정보를 수집한 결과입니다.");
            sb.AppendLine();
            for (var i = 0; i < cafeList.Count; i++)
            {
                var cafe = cafeList[i];
                sb.AppendLine($"{i + 1}. 카페주제:{{{rootCategoryUsed}}} 카페이름:{{{cafe.cafeName}}} 카페이름영문:{{{cafe.cafeNameEng}}} 카페주소:{{{cafe.cafeUrl}}} 카페ID:{{{cafe.cafeId}}} 카페회원수:{{{cafe.memberCount}}} 카페개설일:{{{cafe.cafeOpenDate}}} 카페매니저닉네임:{{{cafe.ownerName}}} 카페매니저ID:{{{cafe.ownerId}}}");
            }
            var savePath = $"{Environment.CurrentDirectory}\\{Environment.TickCount}.txt";
            File.WriteAllText(savePath, sb.ToString());
            Console.WriteLine($"\n{savePath} 위치에 저장 되었습니다.");
            Console.WriteLine("엔터치면 종료합니다.");
            Console.ReadLine();
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            var name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
            var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));
            if (resources.Any())
            {
                var resourceName = resources.First();
                using (var stream = thisAssembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }
                    var block = new byte[stream.Length];
                    stream.Read(block, 0, block.Length);
                    return Assembly.Load(block);
                }
            }
            return null;
        }
    }
}