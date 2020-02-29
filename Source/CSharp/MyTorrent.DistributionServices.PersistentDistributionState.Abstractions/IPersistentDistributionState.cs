using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    public interface IPersistentDistributionState
    {
        public bool Commited { get; }

        public string? HashAlgorithm { get; set; }

        public long? FragmentSize { get; set; }

        public IDictionary<string, IFragmentedFileInfo> FileInfos { get; set; }

        public Task CommitAsync(bool autoRevert = true, CancellationToken cancellationToken = default);

        public Task RevertAsync(CancellationToken cancellationToken = default);
    }
}
