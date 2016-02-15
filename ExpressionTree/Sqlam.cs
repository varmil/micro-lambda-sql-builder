using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionTree
{
    /// <summary>
    /// SQL Builder Library. WHERE句以降のクエリだけを出力対象にすることで小さく保つ
    /// </summary>
    public class Sqlam<T> where T : IOrm
    {
        /// <summary>構築したクエリ文字列</summary>
        public string Query { get { return parser.Query; } }

        /// <summary>構築したクエリに紐づくパラメタ辞書</summary>
        public IDictionary<string, object> Parameters { get { return parser.Parameters; } }

        /// <summary>式木を解析してSQL文字列に変換、格納するクラス</summary>
        private ExpressionParser parser = new ExpressionParser();

        // TODO: シャーディングされたテーブルネームを考慮する？
        public Sqlam(Expression<Func<T, bool>> where = null, string tableName = null)
        {
            if (where != null)
            {
                And(where);
            }
        }

        /// <summary>
        /// 左辺にフィールド、右辺に値を入れてください。
        /// 複数条件の場合はこのメソッドをその回数分呼び出してください。
        /// ex)
        ///     NG: .And(e => e.Id > 123 && e.FirstName == "John")
        ///     OK: .And(e => e.Id > 123).And(e => e.FirstName == "John")
        /// </summary>
        public Sqlam<T> And(Expression<Func<T, bool>> where)
        {
            var body = where.Body as BinaryExpression;
            if (body == null)
            {
                throw new ArgumentException("expression is not BinaryExpression. : " + where.Body.NodeType);
            }

            parser.And();
            parser.ParseAnd(body);
            return this;
        }

        /// <summary>WHERE IN</summary>
        public Sqlam<T> In(Expression<Func<T, object>> field, IEnumerable<object> values)
        {
            var fieldName = ExprNameResolver.GetExprName(field.Body);
            parser.And();
            parser.ParseIsIn(fieldName, values);
            return this;
        }

        /// <summary>複数条件の場合はこのメソッドをその回数分呼び出してください。</summary>
        public Sqlam<T> OrderBy(Expression<Func<T, object>> field, Order order = Order.Asc)
        {
            var fieldName = ExprNameResolver.GetExprName(field.Body);
            parser.ParseOrderBy(fieldName, order);
            return this;
        }

        public Sqlam<T> Limit(int number)
        {
            parser.ParseLimit(number);
            return this;
        }

        public Sqlam<T> Offset(int number)
        {
            parser.ParseOffset(number);
            return this;
        }
    }


    public enum Order
    {
        Asc,
        Desc
    }


    /// <summary>
    /// 式木をparseするためのクラス。SQL生成に特化しています。
    /// </summary>
    class ExpressionParser /*: ExpressionVisitor*/
    {
        /// <summary>クエリ文字列とパラメタを格納した辞書</summary>
        public string Query { get { return adapter.QueryString(Conditions, OrderBy, Limit, Offset); } }
        public IDictionary<string, object> Parameters { get; private set; }

        private Dictionary<ExpressionType, string> operatorDict = new Dictionary<ExpressionType, string>()
                                                                {
                                                                    { ExpressionType.Equal, "="},
                                                                    { ExpressionType.NotEqual, "!="},
                                                                    { ExpressionType.GreaterThan, ">"},
                                                                    { ExpressionType.LessThan, "<"},
                                                                    { ExpressionType.GreaterThanOrEqual, ">="},
                                                                    { ExpressionType.LessThanOrEqual, "<="},
                                                                    { ExpressionType.AndAlso, "AND"},
                                                                    { ExpressionType.OrElse, "OR"}
                                                                };

        private List<string> conditions = new List<string>();
        private string Conditions
        {
            get
            {
                if (conditions.Count == 0)
                    return "";
                else
                    return "WHERE " + string.Join("", conditions);
            }
        }

        private List<string> sortList = new List<string>();
        private string OrderBy
        {
            get
            {
                if (sortList.Count == 0)
                    return "";
                else
                    return "ORDER BY " + string.Join(", ", sortList);
            }
        }

        private int limit;
        private string Limit
        {
            get
            {
                if (limit == default(int))
                    return "";
                else
                    return "LIMIT " + limit;
            }
        }

        private int offset;
        private string Offset
        {
            get
            {
                if (offset == default(int))
                    return "";
                else
                    return "OFFSET " + offset;
            }
        }

        /// <summary>SQLパラメタ部分の文字列に使用</summary>
        private static readonly string PARAMETER_PREFIX = "__P";
        private int paramIndex = 0;

        /// <summary>dapper以外のライブラリに移行した時のために念のためアダプタを作成</summary>
        private ISqlAdapter adapter;

        public ExpressionParser()
        {
            this.Parameters = new ExpandoObject();
            this.adapter = new DapperAdapter();
        }

        /// <summary>
        /// 二項演算子。左辺にフィールド、右辺に値。WHERE条件式の構築に使用
        /// </summary>
        public void ParseAnd(BinaryExpression node)
        {
            // 左辺チェック
            var fieldName = ExprNameResolver.GetExprName(node.Left);

            // 右辺チェック。リテラル、フィールド、プロパティなどExpressionTypeが様々なのでResolverを使う
            var fieldValue = ExprValueResolver.GetExprValue(node.Right);

            // オペレータチェック
            string oper;
            if (!operatorDict.TryGetValue(node.NodeType, out oper))
            {
                throw new KeyNotFoundException("Suitable operator not found. Key is " + node.NodeType);
            }

            QueryByField(fieldName, oper, fieldValue);
        }

        public void ParseIsIn(string fieldName, IEnumerable<object> values)
        {
            QueryByIsIn(fieldName, values);
        }

        public void ParseOrderBy(string fieldName, Order order)
        {
            QueryByOrder(fieldName, order);
        }

        public void ParseLimit(int limit)
        {
            this.limit = limit;
        }

        public void ParseOffset(int offset)
        {
            this.offset = offset;
        }

        public void And()
        {
            if (conditions.Count > 0) conditions.Add(" AND ");
        }

        private void QueryByField(string fieldName, string op, object fieldValue)
        {
            var paramId = NextParamId();
            var newCondition = string.Format("{0} {1} {2}", adapter.Field(fieldName), op, adapter.Parameter(paramId));
            AddParameter(paramId, fieldValue);
            conditions.Add(newCondition);
        }

        private void QueryByIsIn(string fieldName, IEnumerable<object> values)
        {
            var paramIds = values.Select(x =>
            {
                var paramId = NextParamId();
                AddParameter(paramId, x);
                return adapter.Parameter(paramId);
            });

            var newCondition = string.Format("{0} IN ({1})", adapter.Field(fieldName), string.Join(",", paramIds));
            conditions.Add(newCondition);
        }

        private void QueryByOrder(string fieldName, Order order)
        {
            fieldName = adapter.Field(fieldName);
            if (order == Order.Desc) fieldName += " DESC";
            sortList.Add(fieldName);
        }

        private string NextParamId()
        {
            ++paramIndex;
            return PARAMETER_PREFIX + paramIndex.ToString(CultureInfo.InvariantCulture);
        }

        private void AddParameter(string key, object value)
        {
            if (!Parameters.ContainsKey(key)) Parameters.Add(key, value);
        }
    }


    /// <summary>プロパティやフィールド、関数コール結果の「値」を取得する。 "lambda-sql-builder" をベースに改善</summary>
    static class ExprValueResolver
    {
        public static object GetExprValue(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return (expression as ConstantExpression).Value;
                case ExpressionType.Convert:
                    return GetExprValue((expression as UnaryExpression).Operand);
                case ExpressionType.Call:
                    return ResolveMethodCall(expression as MethodCallExpression);
                case ExpressionType.MemberAccess:
                    var memberExpr = (expression as MemberExpression);
                    var obj = memberExpr.Expression != null ? GetExprValue(memberExpr.Expression) : null;
                    return ResolveValue((dynamic)memberExpr.Member, obj);
                default:
                    throw new ArgumentException("No suitable ExpressionType found : " + expression.NodeType);
            }
        }

        private static object ResolveMethodCall(MethodCallExpression callExpression)
        {
            var arguments = callExpression.Arguments.Select(GetExprValue).ToArray();
            var obj = callExpression.Object != null ? GetExprValue(callExpression.Object) : arguments.First();
            return callExpression.Method.Invoke(obj, arguments);
        }

        private static object ResolveValue(PropertyInfo property, object obj)
        {
            return property.GetValue(obj, null);
        }

        private static object ResolveValue(FieldInfo field, object obj)
        {
            return field.GetValue(obj);
        }
    }


    /// <summary>プロパティ名やフィールド名を文字列で取得したい時に使用する</summary>
    static class ExprNameResolver
    {
        public static string GetExprName(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Convert:
                    var unaryExpr = (expression as UnaryExpression);
                    return ((MemberExpression)unaryExpr.Operand).Member.Name;
                case ExpressionType.MemberAccess:
                    var memberExpr = (expression as MemberExpression);
                    return memberExpr.Member.Name;
                default:
                    throw new ArgumentException("No suitable ExpressionType found : " + expression.NodeType);
            }
        }
    }
}
