using CenterApp.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Service.IRepository
{
    public interface IStudentAccountService { Task<OpResult> EnsureAsync(long studentId); }

}
