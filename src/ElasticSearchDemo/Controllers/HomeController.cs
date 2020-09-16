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

        public IActionResult Index(SearchQueryModel model)
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
            var searchResponseAggOLD = _elasticClient.Search<PersonFullDetails>(s => s
                .From((model.Page - 1) * model.PageSize)
                .Size(model.PageSize)
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
                    .Terms("roles", ta => ta
                        .Field(f => f.Roles.Suffix("keyword"))
                    )
                    .Terms("company_names", ta => ta
                        .Field(f => f.Company.Name.Suffix("keyword"))
                    )
                )
            );

            var searchQuery = new SearchDescriptor<PersonFullDetails>()
                .From((model.Page - 1) * model.PageSize)
                .Size(model.PageSize);
            //Term
            /*searchQuery = searchQuery
                .Query(qu => qu
                    .Bool(b => b
                        //Term
                        .Must(must => must
                            .Match(m => m
                                .Field(f => f.Firstname)
                                .Query(model.Term)
                                )
                            )
                        //Filter
                        .Filter(f =>
                            f.Match(term => term//Term?
                                .Field(field => field.Lastname)
                                .Query("Dupont")//Value?
                            )
                        )
                    )
                );*/

            //TODO: Clean next lines
            var lastnames = model.LastnameFilterValues ?? new string[] { };
            var companies = model.CompanyFilterValues ?? new string[] { };
            var roles = model.RoleFilterValues ?? new string[] { };
            var filters = new List<Func<QueryContainerDescriptor<PersonFullDetails>, QueryContainer>>();
            //TODO: Include 1+ filters
            if (lastnames.Any(i=> i != null))
            {
                foreach (var lastname in lastnames)
                {
                    filters.Add(fq => fq.Match(t => t.Field(f => f.Lastname).Query(lastname)));
                }
            }
            if (companies.Any(i => i != null))
            {
                foreach (var company in companies)
                {
                    filters.Add(fq => fq.Match(t => t.Field(f => f.Company.Name).Query(company)));
                }
            }
            if (roles.Any(i => i != null))
            {
                foreach (var role in roles)
                {
                    filters.Add(fq => fq.Match(t => t.Field(f => f.Roles).Query(role)));
                }
            }

            Fields firstnameField = Infer.Field<PersonFullDetails>(p => p.Firstname);
            var lastnameField = Infer.Field<PersonFullDetails>(p => p.Lastname, 2);//Boost 2, more important
            var bioField = Infer.Field<PersonFullDetails>(p => p.Bio);
            searchQuery = searchQuery
                .Query(qu => qu
                    .Bool(b => b
                        //Term
                        .Must(must => must
                            //.Match(m => m
                            .MultiMatch(m => m
                                //.Field(f => f.Firstname)
                                .Fields(firstnameField.And(lastnameField).And(bioField))
                                .Query(model.Term)
                                )
                            )
                        //Filter
                        .Filter(filters)
                        /*.Filter(f =>
                            f.Match(term => term//Term?
                                .Field(field => field.Lastname)
                                .Query(model.NameFilterValue)//Value?
                            )
                        )*/
                    )
                );
            
            //Aggregates
            searchQuery = searchQuery
                .Aggregations(a => a
                    .Terms("last_names", ta => ta
                        .Field(f => f.Lastname.Suffix("keyword"))
                    )
                    .Terms("roles", ta => ta
                        .Field(f => f.Roles.Suffix("keyword"))
                    )
                    .Terms("company_names", ta => ta
                        .Field(f => f.Company.Name.Suffix("keyword"))
                    )
                );
            var searchResponseAgg = _elasticClient.Search<PersonFullDetails>(searchQuery);

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
                    Lastname = document.Lastname,
                    Age = document.Age,
                    Bio = document.Bio,
                    Roles = document.Roles,
                    Company = new SearchCompanyModel
                    {
                        Id = document.Company.Id,
                        Name = document.Company.Name
                    }
                });
            }

            var filterGroups = new List<SearchFilterGroup>();
            //Lastname
            var lastnameFilters = new List<SearchFilter>();
            foreach (var bucket in searchResponseAgg.Aggregations.Terms("last_names").Buckets)
            {
                lastnameFilters.Add(new SearchFilter
                {
                    Label = bucket.Key,
                    Count = bucket.DocCount ?? 0
                });
            }
            var lastnameFilterGroup = new SearchFilterGroup
            {
                Label = "Lastname",
                Filters = lastnameFilters
            };
            filterGroups.Add(lastnameFilterGroup);
            //Role
            var roleFilters = new List<SearchFilter>();
            foreach (var bucket in searchResponseAgg.Aggregations.Terms("roles").Buckets)
            {
                roleFilters.Add(new SearchFilter
                {
                    Label = bucket.Key,
                    Count = bucket.DocCount ?? 0
                });
            }
            var roleFilterGroup = new SearchFilterGroup
            {
                Label = "Role",
                Filters = roleFilters
            };
            filterGroups.Add(roleFilterGroup);
            //Company
            var companyFilters = new List<SearchFilter>();
            foreach (var bucket in searchResponseAgg.Aggregations.Terms("company_names").Buckets)
            {
                companyFilters.Add(new SearchFilter
                {
                    Label = bucket.Key,
                    Count = bucket.DocCount ?? 0
                });
            }
            var companyFilterGroup = new SearchFilterGroup
            {
                Label = "Company",
                Filters = companyFilters
            };
            filterGroups.Add(companyFilterGroup);

            var totalPages = searchResponseAgg.Total / (double)model.PageSize;
            var data = new SearchResultModel
            {
                SearchTerm = model.Term,
                NbTotalResults = searchResponseAgg.Total,
                Results = results,
                FilterGroups = filterGroups,
                PageSize = model.PageSize,
                CurrentPage = model.Page,
                TotalPages = (int)Math.Ceiling(totalPages),
                LastnameFilterValues = model.LastnameFilterValues,
                CompanyFilterValues = model.CompanyFilterValues,
                RoleFilterValues = model.RoleFilterValues
            };
            return View(data);
        }

        public IActionResult Populate()
        {
            //TODO: Add Lorem ipsum description for full-text search with highlighting and score
            //Add data
            var person1 = new PersonFullDetails
            {
                Id = 1,
                Firstname = "Gilles",
                Lastname = "Lautrou",
                Age = 30,
                Bio = "Sites internet, applications métiers, intranets collaboratifs, solutions de mobilité. Webnet rassemble 140 ingénieurs, consultants, experts des technologies internet. Ethique et diversité. 140 experts du digital. Culture de l’innovation. Guidé par vos objectifs.",
                Roles = new List<string> { "Developer", "Architect", "Manager" },
                Company = new PersonFullDetailsCompany
                {
                    Id = 1,
                    Name = "Webnet"
                }
            };
            var person2 = new PersonFullDetails
            {
                Id = 2,
                Firstname = "Jean-Pierre",
                Lastname = "Dupont",
                Age = 36,
                Bio = "Microsoft, en tant qu'acteur de la transformation numérique en France, aide les individus et les entreprises du monde entier à exploiter pleinement leur potentiel.",
                Roles = new List<string> { "Developer" },
                Company = new PersonFullDetailsCompany
                {
                    Id = 1,
                    Name = "Webnet"
                }
            };
            var person3 = new PersonFullDetails
            {
                Id = 3,
                Firstname = "Claude",
                Lastname = "Dupont",
                Age = 42,
                Bio = "Planifiez plus intelligemment, collaborez mieux et livrez plus rapidement avec Azure DevOps Services, anciennement connu sous le nom Visual Studio Team",
                Roles = new List<string> { "Developer", "DevOps" },
                Company = new PersonFullDetailsCompany
                {
                    Id = 2,
                    Name = "Microsoft"
                }
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
