using System.Threading.Tasks;

namespace AcgFotos.Core.Data {

    public class UnitOfWork : IUnitOfWork {

        private readonly IDbContext _dDbContext;

        public UnitOfWork(IDbContext dDbContext) {
            _dDbContext = dDbContext;
        }

        public void Commit() {
            _dDbContext.Commit();
        }

        public Task CommitAsync() {
            return _dDbContext.CommitAsync();
        }
    }
}
