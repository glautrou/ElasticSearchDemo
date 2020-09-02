using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ElasticSearchDemo.Models;
using ElasticSearchDemo.Models.ElasticSearch;
using Nest;
using Elasticsearch.Net;

namespace ElasticSearchDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IElasticClient _elasticClient;

        public HomeController(ILogger<HomeController> logger, IElasticClient elasticClient)
        {
            _logger = logger;
            _elasticClient = elasticClient;
        }

        public IActionResult Index(int pageSize = 10, int page = 1)
        {
            //Search data
            //http://localhost:9200/person_full_details/_search
            //Search exact term (case-insensitive)
            /*var searchResponse = _elasticClient.Search<PersonFullDetails>(s => s
                .From(0)
                .Size(10)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Firstname)
                        .Query("gil*")
                    )
                )
            );*/
            //Search start with
            var searchResponse = _elasticClient.Search<PersonFullDetails>(s => s
                .From(0)
                .Size(10)
                .Query(q => q
                    .Prefix(m => m
                        .Field(f => f.Firstname)
                        .Value("gil")
                    )
                )
            );
            var people = searchResponse.Documents;

            //Low-level search
            var searchResponseLowLevel = _elasticClient.LowLevel.Search<SearchResponse<PersonFullDetails>>(PostData.Serializable(new
            {
                from = 0,
                size = 10,
                query = new
                {
                    match = new
                    {
                        field = "firstname",
                        query = "gilles"
                    }
                }
            }));
            var responseJsonLowLevel = searchResponseLowLevel;

            //Aggregates
            //TODO: Order bu last/first
            var searchResponseAgg = _elasticClient.Search<PersonFullDetails>(s => s
                .From((page - 1) * pageSize)
                .Size(pageSize)
                //.Size(0)
                .Query(q => q
                    .MatchAll()
                /*.Match(m => m
                    .Field(f => f.Firstname)
                    .Query("gilles")
                )*/
                )
                .Aggregations(a => a
                    .Terms("last_names", ta => ta
                        .Field(f => f.Lastname.Suffix("keyword"))
                    )
                )
            );

            /*var search2 = new SearchDescriptor<PersonFullDetails>()
               .Query(q => q
                   .QueryString(queryString => queryString
                       .Query("gilles")))
               .Aggregations(a => a
                   .Terms("last_names", term => term
                       .Field(f => f.Lastname.Suffix("keyword"))));*/

            //var search21 = _elasticClient.Search<PersonFullDetails>(search2);

            var results = new List<SearchPersonModel>();
            foreach (var document in searchResponseAgg.Documents)
            {
                results.Add(new SearchPersonModel
                {
                    Id = document.Id,
                    Firstname = document.Firstname,
                    Lastname = document.Lastname
                });
            }

            var filters = new List<SearchFilter>();
            //TODO: Add more groups
            foreach (var bucket in searchResponseAgg.Aggregations.Terms("last_names").Buckets)
            {
                filters.Add(new SearchFilter
                {
                    Label = bucket.Key,
                    Count = bucket.DocCount ?? 0
                });
            }

            var totalPages = searchResponseAgg.Total / (double)pageSize;
            var model = new SearchModel
            {
                SearchTerm = string.Empty,
                NbTotalResults = searchResponseAgg.Total,
                Results = results,
                Filters = filters,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalPages)
            };
            return View(model);
        }

        public IActionResult Populate()
        {
            //Add data
            var person1 = new PersonFullDetails
            {
                Id = 1,
                Firstname = "Gilles",
                Lastname = "Lautrou"
            };
            var person2 = new PersonFullDetails
            {
                Id = 2,
                Firstname = "Jean-Pierre",
                Lastname = "Dupont"
            };
            var person3 = new PersonFullDetails
            {
                Id = 3,
                Firstname = "Claude",
                Lastname = "Dupont"
            };

            var data = new[]
            {
                person1,
                person2,
                person3
            };

            _elasticClient.Bulk(b => b
                .IndexMany(data)
                .Refresh(Refresh.WaitFor));

            //var response1 = _elasticClient.IndexDocument(person1);
            //var response2 = _elasticClient.IndexDocument(person2);
            //var response3 = _elasticClient.IndexDocument(person3);

            return Ok("Populated with some data");
        }
    }
}
