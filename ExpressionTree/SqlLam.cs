using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionTree
{
    /// <summary>
    /// SQL Builder Library. WHERE句以降のクエリだけを出力対象にすることで小さく保つ予定
    /// </summary>
    public class SqlLam<T> where T : IOrm
    {
        // WHERE句以降のクエリ文字列
        public string Query
        {
            get
            {
                where = (where != null) ? " WHERE " + where : null;
                orderBy = (orderBy != null) ? " ORDER BY " + orderBy : null;
                limit = (limit != null) ? " LIMIT " + limit : null;
                offset = (offset != null) ? " OFFSET " + offset : null;
                return where + orderBy + limit + offset;
            }
        }

        private string where;
        private string orderBy;
        private string limit;
        private string offset;

        public SqlLam(Expression<Func<T, bool>> expression = null)
        {
            if (expression != null)
            {
                And(expression);
            }
        }

        public SqlLam<T> And(Expression<Func<T, bool>> expression)
        {
            // 1つ目の条件の前には"AND"は不要
            if (where != null)
            {
                where += " AND ";
            }

            // 式木を用いてラムダ式をパース
            var visitor = new ParameterVisitor();
            visitor.Visit(expression.Body);

            // 文字列を追加
            where += visitor.Query;
            return this;
        }

        // TODO: WHERE ~ IN
        public SqlLam<T> In(Expression<Func<T, object>> field, object values)
        {
            return this;
        }

        // TODO: ORDER BY
        public SqlLam<T> OrderBy(Expression<Func<T, object>> field, bool descending = false)
        {
            return this;
        }

        // TODO: LIMIT
        public SqlLam<T> Limit(int number)
        {
            return this;
        }

        // TODO: Offset
        public SqlLam<T> Offset(int number)
        {
            return this;
        }
    }


    /// <summary>
    /// 式木をparseするためのクラス。VisitorPatternを使用。SQL生成に特化しています。
    /// 参考：http://qiita.com/takeshik/items/438b845154c6c21fcba5
    /// </summary>
    public class ParameterVisitor : ExpressionVisitor
    {
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

        // WHERE句以降のクエリ文字列
        public string Query { get; private set; }

        public ParameterVisitor()
        {

        }

        /// <summary>二項演算子。これが処理の起点になるケースが多いはず</summary>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            // 左辺、演算子、右辺の順にConstant or Operatorに行き着くまで再帰的に処理する
            this.Visit(node.Left);

            string sqlOperator;
            if (!operationDictionary.TryGetValue(node.NodeType, out sqlOperator))
            {
                throw new KeyNotFoundException("Suitable operator not found. Key is " + node.NodeType.ToString());
            }
            this.Query += sqlOperator + " ";

            this.Visit(node.Right);

            return node;
        }

        /// <summary>定数値。式木をパースしていった時の終着点</summary>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            // 数値型、文字列型等で、クォートが変化する
            // TODO: クォート処理が面倒なのでDapperに頼る。<string, object> の辞書にパラメタを詰める。
            // AddParameter(), NextParamId() あたりを参考に。つまりkeyとなる文字列はカラム名と一致する必要はなく単なるIndexで良いのである。
            // 参考：https://github.com/varmil/lambda-sql-builder/blob/fc479757251aacfbea74cb503782cbf244dec83e/LambdaSqlBuilder/Builder/SqlQueryBuilderExpr.cs#L45
            // 参考：https://github.com/varmil/lambda-sql-builder/blob/fc479757251aacfbea74cb503782cbf244dec83e/LambdaSqlBuilder/SqlLamBase.cs#L35
            //var type = node.Type;
            //if (type == typeof(string))
            //{
            //    Console.WriteLine("string!");
            //}
            //else if (type == typeof(int))
            //{
            //    Console.WriteLine("int!");
            //}

            var value = node.Value.ToString();
            this.Query += value + " ";
            return node;
        }

        /// <summary>Field or Propertyへのアクセス</summary>
        protected override Expression VisitMember(MemberExpression node)
        {
            // 文字列リテラルとして返す
            var name = node.Member.Name;
            return this.Visit(Expression.Constant(name));
        }

        //protected override Expression VisitUnary(UnaryExpression node)
        //{
        //    return node;
        //}

        //protected override Expression VisitParameter(ParameterExpression node)
        //{
        //    return node;
        //}
    }
}
