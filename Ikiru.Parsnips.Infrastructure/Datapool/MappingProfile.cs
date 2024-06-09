using AutoMapper;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using Ikiru.Parsnips.Infrastructure.Datapool.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Infrastructure.Datapool
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<DataPoolPersonSearchResults<Person>, PersonSearchResults<Person>>();
        }
    }
}
