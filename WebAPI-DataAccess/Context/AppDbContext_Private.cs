using Dapper.Repositories;

namespace WebAPI_DataAccess.Context
{
    public partial class ApplicationDbContext
    {
        #region Private Repository Properties
        //private IDapperRepository<Phone> _phones;

        private IDapperSProcRepository _spRepo;
        #endregion
    }
}
