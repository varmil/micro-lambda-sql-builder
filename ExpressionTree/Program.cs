using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

// BinaryExpression（二項演算）, UnaryExpression（単項式）, ConstantExpression（定数）
namespace ExpressionTree
{
    class Program
    {
        static void Main(string[] args)
        {
            // "WHERE - IN" の書き方例
            // ServiceStack.OrmLite: q => Sql.In(q.City, "London", "Madrid", "Berlin")
            // lambda-sql-builder: WhereIsIn(c => c.CategoryName, new object[] { "Beverages", "Condiments" })

            // "WHERE id = 1 and first_name = John", "WHERE id > 10"
            Expression<Func<Employee, bool>> exp = (e) => e.Id > 123 && e.FirstName == "John" && e.LastName == "Abc" && e.Id == 456;
            var visitor = new ParameterVisitor();
            visitor.Visit(exp.Body);
        }
    }

    /// <summary>
    /// 式木をparseするためのクラス
    /// 参考：http://qiita.com/takeshik/items/438b845154c6c21fcba5
    /// </summary>
    public class ParameterVisitor : ExpressionVisitor
    {
        public ParameterVisitor()
        {

        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            // ~~可変個の条件式が渡ってくるので、再帰的にパースできるような実装をする~~ → 下記のようにメソッドチェインの制約を付与すれば楽か
            // lambda-sql-builder: new SqlLam<Employee>(p => p.Id > 1).And(p => p.Title != "Sales Representative") 
            return null;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return null;
        }


        protected override Expression VisitUnary(UnaryExpression node)
        {
            return null;
        }
    }

    /// <summary>
    /// SQL Builder Library. WHERE句以降のクエリだけを出力対象にすることで小さく保つ予定
    /// </summary>
    public class SqlLam<T> where T : IOrm
    {
        // Visitor pattern
        ParameterVisitor visitor = new ParameterVisitor();

        // WHERE句以降のクエリ文字列
        public string Query { get; private set; }

        // TODO: コンストラクタでもWHERE条件を受け付けるかも
        public SqlLam(Expression<Func<T, bool>> expression)
        {
        }

        public SqlLam<T> And(Expression<Func<T, bool>> expression)
        {
            visitor.Visit(expression.Body);
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

        // TODO: number
        public SqlLam<T> Offset(int number)
        {
            return this;
        }
    }

    public class Employee : IOrm
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public interface IOrm
    {
    }
}