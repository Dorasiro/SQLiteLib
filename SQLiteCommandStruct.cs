using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteLib
{
    public struct SQLiteLibCommandAddParam
    {
        public string cmd;
        public List<SQLiteParameter> param;

        public SQLiteLibCommandAddParam(string cmd, List<SQLiteParameter> param)
        {
            this.cmd = cmd.Replace("\"", "'");
            this.param = param;
        }
    }
}
