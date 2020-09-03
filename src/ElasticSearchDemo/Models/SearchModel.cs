using System.Collections.Generic;

namespace ElasticSearchDemo.Models
{
    public class SearchModel
    {
        public string SearchTerm { get; set; }
        public long NbTotalResults { get; set; }
        public List<SearchPersonModel> Results;
        public List<SearchFilterGroup> FilterGroups;
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class SearchPersonModel
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int? Age { get; set; }
        public List<string> Roles { get; set; }
        public SearchCompanyModel Company { get; set; }
    }

    public class SearchCompanyModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SearchFilterGroup
    {
        public string Label { get; set; }
        public List<SearchFilter> Filters { get; set; }
    }

    public class SearchFilter
    {
        public string Label { get; set; }
        public long Count { get; set; }
    }
}
