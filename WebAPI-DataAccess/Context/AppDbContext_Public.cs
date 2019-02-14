using Dapper.Repositories;

namespace WebAPI_DataAccess.Context
{
    public partial class ApplicationDbContext
    {
        #region Public Repository Properties
        //public IDapperRepository<Phone> Phones => _phones ?? (_phones = new DapperRepository<Phone>(Connection, _config));

        public IDapperSProcRepository StoreProcedureRepo => _spRepo ?? (_spRepo = new DapperSProcRepository(Connection));
        #endregion

    }
}
