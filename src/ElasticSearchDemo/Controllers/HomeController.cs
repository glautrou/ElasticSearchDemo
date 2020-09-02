using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ElasticSearchDemo.Models;
using Nest;
using Elasticsearch.Net;

namespace ElasticSearchDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //Connect
            var uri = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(uri)
                .DefaultMappingFor<Person>(m => m
                    .IndexName("person_full3")
                );
            var client = new ElasticClient(settings);

            var createIndexResponse = client.Indices.Create("person_full2", c => c
                .Map<Person>(m => m.AutoMap())
            );

            var createIndexResponse2 = client.Indices.Create("person_full3", c => c
                .Map<Person>(m => m
                    .AutoMap() //POCO/Attribute mapping
                    .Properties(p => p
                        .Text(t => t
                            .Name(n => n.LastName)
                            .Fields(ff => ff
                                .Text(tt => tt
                                    .Name("stop")
                                    .Analyzer("stop")
                                )
                                .Text(tt => tt
                                    .Name("shingles")
                                    .Analyzer("name_shingles")
                                )
                                .Keyword(k => k
                                    .Name("keyword")
                                    .IgnoreAbove(256)
                                )
                            )
                        )
                    )
                )
            );

            //Add data
            var person1 = new Person
            {
                Id = 1,
                FirstName = "Gilles",
                LastName = "Lautrou"
            };
            var person2 = new Person
            {
                Id = 2,
                FirstName = "Jean",
                LastName = "Dupont"
            };
            var person3 = new Person
            {
                Id = 3,
                FirstName = "Claude",
                LastName = "Dupont"
            };

            var response1 = client.IndexDocument(person1);
            var response2 = client.IndexDocument(person2);
            var response3 = client.IndexDocument(person3);

            //Search data
            //http://localhost:9200/person_full/_search
            //Search exact term (case-insensitive)
            /*var searchResponse = client.Search<Person>(s => s
                .From(0)
                .Size(10)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.FirstName)
                        .Query("gil*")
                    )
                )
            );*/
            //Search start with
            var searchResponse = client.Search<Person>(s => s
                .From(0)
                .Size(10)
                .Query(q => q
                    .Prefix(m => m
                        .Field(f => f.FirstName)
                        .Value("gil")
                    )
                )
            );
            var people = searchResponse.Documents;

            //Low-level search
            var searchResponseLowLevel = client.LowLevel.Search<SearchResponse<Person>>(PostData.Serializable(new
            {
                from = 0,
                size = 10,
                query = new
                {
                    match = new
                    {
                        field = "firstName",
                        query = "gilles"
                    }
                }
            }));
            var responseJsonLowLevel = searchResponseLowLevel;

            //Aggregates
            var searchResponseAgg = client.Search<Person>(s => s
                .Size(0)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.FirstName)
                        .Query("gilles")
                    )
                )
                .Aggregations(a => a
                    .Terms("last_names", ta => ta
                        .Field(f => f.LastName.Suffix("keyword"))
                    )
                )
            );
            var termsAggregation = searchResponseAgg.Aggregations.Terms("last_names");

            //"Invalid NEST response built from a unsuccessful (400) low level call on POST: /person_full/_search?typed_keys=true\n# Audit trail of this API call:\n - [1] BadResponse: Node: http://localhost:9200/ Took: 00:00:00.0540240\n# OriginalException: Elasticsearch.Net.ElasticsearchClientException: Request failed to execute. Call: Status code 400 from: POST /person_full/_search?typed_keys=true. ServerError: Type: search_phase_execution_exception Reason: \"all shards failed\" CausedBy: \"Type: illegal_argument_exception Reason: \"Text fields are not optimised for operations that require per-document field data like aggregations and sorting, so these operations are disabled by default. Please use a keyword field instead. Alternatively, set fielddata=true on [lastName] in order to load field data by uninverting the inverted index. Note that this can use significant memory.\" CausedBy: \"Type: illegal_argument_exception Reason: \"Text fields are not optimised for operations that require per-document field data like aggregations and sorting, so these operations are disabled by default. Please use a keyword field instead. Alternatively, set fielddata=true on [lastName] in order to load field data by uninverting the inverted index. Note that this can use significant memory.\"\"\"\n# Request:\n<Request stream not captured or already read to completion by serializer. Set DisableDirectStreaming() on ConnectionSettings to force it to be set on the response.>\n# Response:\n<Response stream not captured or already read to completion by serializer. Set DisableDirectStreaming() on ConnectionSettings to force it to be set on the response.>\n"
            //Text fields are not optimised for operations that require per-document field data like aggregations and sorting
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    [ElasticsearchType(RelationName = "person")]
    public class Person
    {
        [Number(DocValues = false, IgnoreMalformed = true, Coerce = true)]
        public int Id { get; set; }
        [Text(Name = "first_name")]
        public string FirstName { get; set; }
        //Keyword is need fort sort/aggregation, avoird fielddata: https://www.elastic.co/guide/en/elasticsearch/reference/current/fielddata.html
        [Text(Name = "last_name")]
        public string LastName { get; set; }
    }
}
