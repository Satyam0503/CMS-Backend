namespace Codeji.CMS.Domain.Models
{
    public class FilterInfo
    {
        public FilterInfo()
        {
            tipForDirectors = Array.Empty<bool>();
            categories = Array.Empty<string>();
            cost = Array.Empty<int>();
            difficulty = Array.Empty<int>();
            status = Array.Empty<bool>();
            subCategories = Array.Empty<string>();
        }
        public string[] categories { get; set; }
        public int[] cost { get; set; }
        public int[] difficulty { get; set; }
        public bool[] status { get; set; }
        public string[] subCategories { get; set; }
        public bool[] tipForDirectors { get; set; }
    }
}
