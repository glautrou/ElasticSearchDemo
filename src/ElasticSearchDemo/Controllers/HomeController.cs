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

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //Connect
            var uri = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(uri)
                //Necessary?
                .DefaultMappingFor<PersonFullDetails>(m => m
                    .IndexName("person_full_details")
                );
            var client = new ElasticClient(settings);
            client.MapPersonFullDetails();

            //Search data
            //http://localhost:9200/person_full_details/_search
            //Search exact term (case-insensitive)
            /*var searchResponse = client.Search<PersonFullDetails>(s => s
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
            var searchResponse = client.Search<PersonFullDetails>(s => s
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
            var searchResponseLowLevel = client.LowLevel.Search<SearchResponse<PersonFullDetails>>(PostData.Serializable(new
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
            var searchResponseAgg = client.Search<PersonFullDetails>(s => s
                .Size(0)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Firstname)
                        .Query("gilles")
                    )
                )
                .Aggregations(a => a
                    .Terms("last_names", ta => ta
                        .Field(f => f.Lastname.Suffix("keyword"))
                    )
                )
            );
            var termsAggregation = searchResponseAgg.Aggregations.Terms("last_names");

            return View();
        }

        public IActionResult Populate()
        {
            //Connect
            var uri = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(uri)
                //Necessary?
                .DefaultMappingFor<PersonFullDetails>(m => m
                    .IndexName("person_full_details")
                );
            var client = new ElasticClient(settings);
            client.MapPersonFullDetails();

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
                Firstname = "Jean",
                Lastname = "Dupont"
            };
            var person3 = new PersonFullDetails
            {
                Id = 3,
                Firstname = "Claude",
                Lastname = "Dupont"
            };

            var response1 = client.IndexDocument(person1);
            var response2 = client.IndexDocument(person2);
            var response3 = client.IndexDocument(person3);

            return Ok("Populated with some data");
        }
    }
}
