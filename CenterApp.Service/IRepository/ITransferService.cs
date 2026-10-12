using CenterApp.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Service.IRepository
{
    public interface ITransferService
    {
        Task<List<GroupPick>> PicksAsync(long? studentId);
        Task<TransferFormVm?> FormAsync(long studentId, long? from);
        Task<OpResult> TransferAsync(long studentId, long fromGroupId, long toGroupId);
    }

}
