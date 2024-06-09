namespace Ikiru.Parsnips.Api.RocketReach.Models
{
    /// <summary>
    /// Unused properties are mapped to Json response from rocketreach
    /// </summary>
    public class SearchResponseModel
    {
        public Pagination pagination { get; set; }
        public Profile[] profiles { get; set; }


        public class Pagination
        {
            public int start { get; set; }
            public int next { get; set; }
            public int total { get; set; }
        }

        public class Profile
        {
            public int id { get; set; }
            public string status { get; set; }
            public string name { get; set; }
            public string profile_pic { get; set; }
            public Links links { get; set; }
            public string linkedin_url { get; set; }
            public string location { get; set; }
            public string city { get; set; }
            public object region { get; set; }
            public string country_code { get; set; }
            public string current_title { get; set; }
            public string current_employer { get; set; }
            public Teaser teaser { get; set; }
        }

        public class Links
        {
            public string linkedin { get; set; }
        }

        public class Phone
        {
            public string number { get; set; }
            public string is_premium { get; set; }
        }

        public class Teaser
        {
            public string[] emails { get; set; }
            public Phone[] phones { get; set; }
            public object[] office_phones { get; set; }
            public object[] preview { get; set; }
            public bool is_premium_phone_available { get; set; }
        }

    }
}
