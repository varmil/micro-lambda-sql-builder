using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using System.Data.SqlClient;
using LambdaSqlBuilder;
using System.Diagnostics;
using Humanizer;

namespace ExpressionTree
{
    class Program
    {
        static void Main(string[] args)
        {
            //var conn = new SqlConnection("connectionstring");

            var id = 7770;
            var list = new List<string>() { "A", "BBB", "CCCC" };
            var tomorrow = DateTime.Now.AddDays(1);
            var sql = new Sqlam<Employee>()
                .And(e => e.CreatedAt > DateTime.Now.AddDays(1))
                .In(e => e.FirstName, list)
                .OrderBy(e => e.Id, Order.Desc)
                .OrderBy(e => e.FirstName)
                .Offset(10)
                .Limit(10)
            ;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var sss2 = new Sqlam<Employee>(e => e.Weapon1 == id.ToString());
            //var dog = new Sqlam<Dog>(e => e.Age < 99);

            sw.Stop();
            Console.WriteLine("経過時間の合計 = {0}", sw.Elapsed);
            Console.WriteLine(sql.Query);
            sql.Parameters.Select(p => p.Key + " " + p.Value).ToList().ForEach(s => Console.WriteLine(s));

            Console.ReadKey();
        }
    }

    public class Employee : IOrm
    {
        public int? Id { get; set; }
        public string FirstName { get; set; }
        public string Weapon1 { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class Dog : IOrm
    {
        public string Name { get; set; }
        public int? Age { get; set; }
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