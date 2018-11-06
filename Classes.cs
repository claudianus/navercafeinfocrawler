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
        internal int cafeCount;
        internal int id;
        internal string name;
        internal int parentCategoryId;

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
        internal string cafeNameEng;
        internal string cafeOpenDate;
        internal string cafeUrl;
        internal int memberCount;
        internal string ownerId;
        internal string ownerName;

        internal Cafe(
            string cafeName,
            string cafeNameEng,
            string cafeUrl,
            int cafeId,
            string cafeOpenDate,
            int memberCount,
            string ownerName,
            string ownerId)
        {
            this.cafeName = cafeName;
            this.cafeNameEng = cafeNameEng;
            this.cafeUrl = cafeUrl;
            this.cafeId = cafeId;
            this.cafeOpenDate = cafeOpenDate;
            this.memberCount = memberCount;
            this.ownerName = ownerName;
            this.ownerId = ownerId;
        }
    }
}
