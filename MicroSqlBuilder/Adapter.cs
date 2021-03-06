﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using Humanizer;
using System.Linq;

namespace MicroSqlBuilder
{
    interface ISqlAdapter
    {
        string QueryString(string conditions, string order, string limit, string offset);
        string Field(string fieldName);
        string Parameter(string parameterId);
    }

    public class DapperAdapter : ISqlAdapter
    {
        public string QueryString(string conditions, string order, string limit, string offset)
        {
            IEnumerable<string> clauses = new[] { conditions, order, limit, offset };
            clauses = clauses.Where(s => !string.IsNullOrEmpty(s));
            return string.Join(" ", clauses);
        }

        public string Field(string fieldName)
        {
            // TODO: should use tableName ?  ex) employees.id
            // NOTE: スネークケース＋数値の場合 "user_param1" のように数字の前にはアンダースコアをつけないことに注意
            return string.Format("`{0}`", fieldName.Underscore());
        }

        public string Parameter(string parameterId)
        {
            return "@" + parameterId;
        }
    }
}
