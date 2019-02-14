using System.Data;
using Dapper.Repositories.Attributes;
using Dapper.Repositories.Extensions;

namespace WebAPI_Model
{
    public class GetWebsiteInfoFromDomainModel
    {
        public long WebsiteId { get; set; }

        public int PriorityLanguage { get; set; }

        public string TimeZone { get; set; }

        public string DomainName { get; set; }
    }
}

