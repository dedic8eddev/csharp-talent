namespace Ikiru.Parsnips.Api.RocketReach.Models
{
    public class SearchRequestModel
    {
        public Query query { get; set; }


        public class Query
        {
            public string[] keywords { get; set; }
            public string[]  name { get; set; }
            public string[] current_employer { get; set; }
        }

    }
}
