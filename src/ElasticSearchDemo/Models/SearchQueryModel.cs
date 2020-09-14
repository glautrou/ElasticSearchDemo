using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace ElasticSearchDemo.Models
{
    public class SearchQueryModel
    {
        public string Term { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string[] LastnameFilterValues { get; set; }
        public string[] CompanyFilterValues { get; set; }
        public string[] RoleFilterValues { get; set; }

        public SearchQueryModel()
        {
            Page = 1;
            PageSize = 10;
        }
    }
}
