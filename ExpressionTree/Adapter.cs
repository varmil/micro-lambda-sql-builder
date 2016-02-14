using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionTree
{
    interface ISqlAdapter
    {
        string Field(string tableName, string fieldName);
        string Parameter(string parameterId);
    }

    public class DapperAdapter : ISqlAdapter
    {
        public string Field(string tableName, string fieldName)
        {
            return string.Format("{0}.{1}", tableName, fieldName);
        }

        public string Parameter(string parameterId)
        {
            return "@" + parameterId;
        }

        // TODO: パスカルスネーク変換もここで行う？
    }
}
