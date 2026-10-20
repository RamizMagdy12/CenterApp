using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Application.Abstractions
{
    public interface IMediaTokenService
    {
        string Create(string userId, long fileId);
        bool Validate(string token, string userId, long fileId);
    }

}
