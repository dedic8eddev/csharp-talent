﻿namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person
{
    public class PersonPhoto
    {
        public PersonPhotoUrl Photo { get; set; }

        public class PersonPhotoUrl
        {
            public string Url { get; set; }
        }
    }
}
