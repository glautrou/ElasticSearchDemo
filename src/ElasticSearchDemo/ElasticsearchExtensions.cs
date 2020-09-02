using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using ElasticSearchDemo.Models.ElasticSearch;

namespace ElasticSearchDemo
{
    public static class ElasticsearchExtensions
    {
        public static void AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
        {
            var uri = new Uri(configuration["Elasticsearch:Url"]);

            var settings = new ConnectionSettings(uri)
                //Necessary?
                .DefaultMappingFor<PersonFullDetails>(m => m
                    .IndexName("person_full_details")
                );

            var client = new ElasticClient(settings);

            client.MapPersonFullDetails();

            services.AddSingleton<IElasticClient>(client);
        }
    }
}
