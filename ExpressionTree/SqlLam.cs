using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionTree
{
    /// <summary>
    /// SQL Builder Library. WHERE句以降のクエリだけを出力対象にすることで小さく保つ予定
    /// </summary>
    public class MySqlLam<T> where T : IOrm
    {
        /// <summary>構築したクエリ文字列</summary>
        //public string Query
        //{
        //    get
        //    {
        //        where = (where != null) ? " WHERE " + where : null;
        //        orderBy = (orderBy != null) ? " ORDER BY " + orderBy : null;
        //        limit = (limit != null) ? " LIMIT " + limit : null;
        //        offset = (offset != null) ? " OFFSET " + offset : null;
        //        return where + orderBy + limit + offset;
        //    }
        //}

        /// <summary>構築したクエリに紐づくパラメタ辞書</summary>
        //public IDictionary<string, object> Parameters { get { return visitor. } }

        private string where;
        private string orderBy;
        private string limit;
        private string offset;

        private QueryExpressionVisitor visitor = new QueryExpressionVisitor();

        public MySqlLam(Expression<Func<T, bool>> expression = null)
        {
            if (expression != null)
            {
                And(expression);
            }
        }

        public MySqlLam<T> And(Expression<Func<T, bool>> where)
        {
            var body = where.Body as BinaryExpression;
            if (body == null)
            {
                throw new ArgumentException("expression is not BinaryExpression. : " + where.Body.NodeType.ToString());
            }

            visitor.And();
            visitor.ParseAnd(body);
            return this;
        }

        // TODO: WHERE ~ IN
        public MySqlLam<T> In(Expression<Func<T, object>> field, IEnumerable<object> values)
        {
            var body = field.Body;
            if (!(body is UnaryExpression) && !(body is MemberExpression))
            {
                throw new ArgumentException("expressionType is not acceptable. : " + field.Body.NodeType.ToString());
            }

            var fieldName = visitor.GetNodeName((dynamic)body);
            visitor.And();
            visitor.ParseIsIn(fieldName, values);
            return this;
        }

        /// <summary>複数条件の場合はこのメソッドをその回数分呼び出してください。</summary>
        public MySqlLam<T> OrderBy(Expression<Func<T, object>> field, bool descending = false)
        {
            var body = field.Body;
            if (!(body is UnaryExpression) && !(body is MemberExpression))
            {
                throw new ArgumentException("expressionType is not acceptable. : " + field.Body.NodeType.ToString());
            }

            var fieldName = visitor.GetNodeName((dynamic)body);
            visitor.ParseOrderBy(fieldName, descending);
            return this;
        }

        // TODO: LIMIT
        public MySqlLam<T> Limit(int number)
        {
            return this;
        }

        // TODO: Offset
        public MySqlLam<T> Offset(int number)
        {
            return this;
        }
    }


    /// <summary>
    /// 式木をparseするためのクラス。VisitorPatternを使用。SQL生成に特化しています。
    /// 参考：http://qiita.com/takeshik/items/438b845154c6c21fcba5
    /// </summary>
    public class QueryExpressionVisitor /*: ExpressionVisitor*/
    {
        /// <summary>クエリ文字列とパラメタを格納した辞書</summary>
        public string Query { get; private set; }
        public IDictionary<string, object> Parameters { get; private set; }

        private Dictionary<ExpressionType, string> operationDictionary = new Dictionary<ExpressionType, string>()
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

        private string Order
        {
            get
            {
                if (sortList.Count == 0)
                    return "";
                else
                    return "ORDER BY " + string.Join(", ", sortList);
            }
        }

        private List<string> conditions = new List<string>();
        private List<string> sortList = new List<string>();

        /// <summary>dapper-dot-netのパラメタ部分の文字列に使用</summary>
        private static readonly string PARAMETER_PREFIX = "__P";
        private int paramIndex = 0;

        private ISqlAdapter adapter;

        public QueryExpressionVisitor()
        {
            this.Parameters = new ExpandoObject();
            this.adapter = new DapperAdapter();
        }

        /// <summary>
        /// 二項演算子。WHERE条件式の構築に使用
        /// 複数条件はサポートしておりません。 ex) NG: e.Id > 123 && e.FirstName == "John"
        /// </summary>
        public void ParseAnd(BinaryExpression node)
        {
            // 左辺チェック
            var left = node.Left as MemberExpression;
            if (left == null)
            {
                throw new NotSupportedException("Left.NodeType is not acceptable. : " + node.Left.NodeType.ToString());
            }
            var fieldName = GetNodeName(left);

            // 右辺チェック
            var rightNodeType = node.Right.NodeType;
            if (rightNodeType != ExpressionType.MemberAccess && rightNodeType != ExpressionType.Constant)
            {
                throw new NotSupportedException("Right.NodeType is not acceptable. : " + node.Right.NodeType.ToString());
            }
            var fieldValue = GetNodeValue((dynamic)node.Right);

            // オペレータチェック
            string oper;
            if (!operationDictionary.TryGetValue(node.NodeType, out oper))
            {
                throw new KeyNotFoundException("Suitable operator not found. Key is " + node.NodeType.ToString());
            }

            QueryByField(fieldName, oper, fieldValue);
        }

        /// <summary>ORDER BYのフィールド指定に使用</summary>
        public void ParseOrderBy(string fieldName, bool descending = false)
        {
            QueryByOrder(fieldName, descending);
        }

        public void ParseIsIn(string fieldName, IEnumerable<object> values)
        {
            QueryByIsIn(fieldName, values);
        }

        public void And()
        {
            if (conditions.Count > 0) conditions.Add(" AND ");
        }

        public string GetNodeName(UnaryExpression node)
        {
            return GetNodeName((MemberExpression)node.Operand);
        }

        public string GetNodeName(MemberExpression node)
        {
            return node.Member.Name;
        }

        /// <summary>MemberExpressionからリフレクションを使ってその値を取得する</summary>
        private object GetNodeValue(MemberExpression node)
        {
            var target = node.Expression != null ? GetNodeValue((dynamic)node.Expression) : null;

            var pi = node.Member as PropertyInfo;
            if (pi != null)
            {
                return pi.GetValue(target, null);
            }
            var fi = node.Member as FieldInfo;
            if (fi != null)
            {
                return fi.GetValue(target);
            }

            throw new NotSupportedException("Unsupported expression type.");
        }

        private object GetNodeValue(ConstantExpression node)
        {
            return node.Value;
        }

        private void QueryByField(string fieldName, string op, object fieldValue)
        {
            var paramId = NextParamId();
            var newCondition = string.Format("{0} {1} {2}", fieldName, op, adapter.Parameter(paramId));
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

            var newCondition = string.Format("{0} IN ({1})", fieldName, string.Join(",", paramIds));
            conditions.Add(newCondition);
        }

        private void QueryByOrder(string fieldName, bool descending = false)
        {
            if (descending) fieldName += " DESC";
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
}
