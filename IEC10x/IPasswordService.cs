using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC10x
{
    public interface IPasswordService
    {
        string GetPassword(string key);
    }
}
