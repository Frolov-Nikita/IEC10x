using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC10x
{
    public class CE303PasswordService : IPasswordService
    {
        private static CE303PasswordService instance;

        public static CE303PasswordService Instance {
            get {
                instance = instance ?? new CE303PasswordService();
                return instance;
            }
        }

        private CE303PasswordService()
        {

        }
        
        public string LastSerial { get; private set; }

        public string GetPassword(string key)
        {
            LastSerial = key;
            return "777777";
        }
    }
}
