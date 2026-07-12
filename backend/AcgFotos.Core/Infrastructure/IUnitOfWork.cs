using System.Threading.Tasks;

namespace AcgFotos.Core.Data {

    public interface IUnitOfWork {

        void Commit();

        Task CommitAsync();
    }
}
