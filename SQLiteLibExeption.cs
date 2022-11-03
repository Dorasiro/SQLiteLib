using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteLib
{
    public class SQLiteLibExeption : Exception
    {
        public SQLiteLibExeption(string message)
            : base(message)
        {

        }
    }
}
