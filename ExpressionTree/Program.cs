using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using System.Data.SqlClient;
using LambdaSqlBuilder;
using System.Diagnostics;

// BinaryExpression（二項演算）, UnaryExpression（単項式）, ConstantExpression（定数）
// TODO: Pascal -- Snake変換など

// "WHERE - IN" の書き方例
// ServiceStack.OrmLite: q => Sql.In(q.City, "London", "Madrid", "Berlin")
// lambda-sql-builder: WhereIsIn(c => c.CategoryName, new object[] { "Beverages", "Condiments" })
namespace ExpressionTree
{
    class Program
    {
        static void Main(string[] args)
        {
            //var conn = new SqlConnection("connectionstring");

            //// MySqlLam
            //Expression<Func<Employee, bool>> exp = (e) => e.Id > 123 && e.FirstName == "John" && e.LastName == "Abc" && e.Id == 456;
            //var sql = new MySqlLam<Employee>(exp);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var id = 0;
            var str = "abcd";
            var list = new List<string>() { "A", "BBB", "CCCC" };
            var sss = new MySqlLam<Employee>(e => e.Id >= id)
                .In(e => e.FirstName, list)
                .And(e => e.LastName != list[0])
                .OrderBy(e => e.Id, true)
                .OrderBy(e => e.FirstName);

            var dog = new MySqlLam<Dog>(e => e.Age < 99);


            sw.Stop();
            Console.WriteLine("経過時間の合計 = {0}", sw.Elapsed);

            return;
        }
    }

    public class Employee : IOrm
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class Dog : IOrm
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public interface IOrm
    {
    }
}


//// [dapper] dictionary param example
//var dict = new Dictionary<string, object>()
//{
//    { "Param1", 10 },
//    { "Param2", "John" }
//};
//var bb = conn.Query<Employee>("SELECT * FROM employee Id = @_1, FirstName = @_2", dict);

// SqlLam
//var query = new SqlLam<Employee>(p => p.FirstName == "John" && p.Id > 10);
//var qs = query.QueryString;
//var qp = query.QueryParameters;