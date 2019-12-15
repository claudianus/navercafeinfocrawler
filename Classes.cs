namespace 네이버카페정보수집
{
    internal class RootCategory
    {
        internal int id;
        internal string name;

        internal RootCategory(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    internal class SubCategory
    {
        internal int id;
        internal string name;
        internal int parentCategoryId;
        internal int cafeCount;

        internal SubCategory(int parentCategoryId, int id, string name, int cafeCount)
        {
            this.parentCategoryId = parentCategoryId;
            this.id = id;
            this.name = name;
            this.cafeCount = cafeCount;
        }
    }

    internal class Cafe
    {
        internal int cafeId;
        internal string cafeName;
        internal string cafeUrl;
        internal string introduction;
        internal int categoryId;
        internal string subCategoryName;
        internal int memberCount;
        internal string ownerId;
        internal string ownerName;
        internal string cafeEngName;

        internal Cafe(
            int cafeId,
            string cafeName,
            string cafeUrl,
            string introduction,
            int categoryId,
            string subCategoryName,
            int memberCount,
            string ownerId,
            string ownerName,
            string cafeEngName)
        {
            this.cafeId = cafeId;
            this.cafeName = cafeName;
            this.cafeUrl = cafeUrl;
            this.introduction = introduction;
            this.categoryId = categoryId;
            this.subCategoryName = subCategoryName;
            this.memberCount = memberCount;
            this.ownerId = ownerId;
            this.ownerName = ownerName;
            this.cafeEngName = cafeEngName;
        }
    }

    internal class CafeStaff {
        internal string managerName;
        internal string managerId;

        internal CafeStaff(string managerName, string managerId)
        {
            this.managerName = managerName;
            this.managerId = managerId;
        }
    }
}
