using System.Data;
using Dapper.Repositories.Attributes;
using Dapper.Repositories.Extensions;

namespace WebAPI_Model
{
    public class GetWebsiteInfoFromDomainParam : ISProcParam
    {
        public string ProcedureName => "sp_GetWebsiteId";

        [ProcedureParam("@domain", DbType.String)]
        public string DomainName { get; set; }
    }
}

