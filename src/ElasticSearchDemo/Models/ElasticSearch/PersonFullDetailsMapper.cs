using Nest;

namespace ElasticSearchDemo.Models.ElasticSearch
{
    public static class PersonFullDetailsMapper
    {
        public static CreateIndexResponse MapPersonFullDetails(this ElasticClient client)
        {
            /*var createIndexResponse = client.Indices.Create("person_full2", c => c
                .Map<Person>(m => m.AutoMap())
            );*/

            return client.Indices.Create("person_full_details", c => c
                .Map<PersonFullDetails>(m => m
                    .AutoMap() //POCO/Attribute mapping
                    .Properties(p => p
                        //Make Lastname keyword searchable
                        .Text(t => t
                            .Name(n => n.Lastname)
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
                        //Make Company name keyword searchable
                        .Text(t => t
                            .Name(n => n.Company.Name)
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
        }
    }
}
