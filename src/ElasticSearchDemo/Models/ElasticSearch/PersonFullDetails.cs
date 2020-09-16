using System.Collections.Generic;
using Nest;

namespace ElasticSearchDemo.Models.ElasticSearch
{
    //[ElasticsearchType(RelationName = "person")]
    public class PersonFullDetails
    {
        [Number(DocValues = false, IgnoreMalformed = true, Coerce = true)]
        public int Id { get; set; }
        //[Text(Name = "first_name")]
        public string Firstname { get; set; }
        //Keyword is need fort sort/aggregation, avoird fielddata: https://www.elastic.co/guide/en/elasticsearch/reference/current/fielddata.html
        //[Text(Name = "last_name")]
        public string Lastname { get; set; }
        public int? Age { get; set; }
        public string Bio { get; set; }
        public List<string> Roles { get; set; }
        public PersonFullDetailsCompany Company { get; set; }
    }

    public class PersonFullDetailsCompany
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
