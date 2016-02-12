using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

// BinaryExpression（二項演算）, UnaryExpression（単項式）, ConstantExpression（定数）
// TODO: Pascal -- Snake変換など
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
            var sql = new SqlLam<Employee>(exp);
            var sql2 = new SqlLam<Employee>(e => e.Id == 777).And(e => e.FirstName == "ANDEXP");
            return;
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