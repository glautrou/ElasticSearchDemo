using System;
namespace ElasticSearchDemo.Models
{
    public class SearchQueryModel
    {
        public string Term { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public SearchQueryModel()
        {
            Page = 1;
            PageSize = 10;
        }
    }
}
