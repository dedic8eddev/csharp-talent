namespace Ikiru.Parsnips.Api.RocketReach.Models
{
    public class LookupProfileResponseModel
    {
        public int id { get; set; }
        public string status { get; set; }
        public string name { get; set; }
        public string profile_pic { get; set; }
        public string linkedin_url { get; set; }
        public Links links { get; set; }
        public string location { get; set; }
        public string current_title { get; set; }
        public object current_employer { get; set; }
        public object current_work_email { get; set; }
        public object current_personal_email { get; set; }
        public LookupProfileResponseEmailModel[] emails { get; set; }
        public LookupProfileResponsePhoneNumberModel[] phones { get; set; }


        public class Links
        {
            public string linkedin { get; set; }
        }


    }
}
