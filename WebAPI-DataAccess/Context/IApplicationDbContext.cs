using Dapper.Repositories;
using Dapper.Repositories.DbContext;

namespace WebAPI_DataAccess.Context
{
    public interface IApplicationDbContext : IDapperDbContext
    {
        IDapperSProcRepository StoreProcedureRepo { get; }

        //IDapperRepository<Phone> Phones { get; }
    }
}
