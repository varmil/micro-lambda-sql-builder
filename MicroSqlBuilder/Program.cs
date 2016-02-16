using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using System.Data.SqlClient;
using LambdaSqlBuilder;
using System.Diagnostics;
using Humanizer;

namespace MicroSqlBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = new List<string>() { "A", "BBB", "CCCC" };
            var sql = new Sqlam<Employee>()
                .And(e => e.CreatedAt > DateTime.Now.AddDays(1))
                .In(e => e.FirstName, list)
                .OrderBy(e => e.Id, Order.Desc)
                .OrderBy(e => e.FirstName)
                .Offset(10)
                .Limit(10);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 10000; i++)
            {
                var str = list[1] + i.ToString();
                var sql2 = new Sqlam<Employee>(e => e.Weapon1 == str);
            }

            sw.Stop();
            Debug.WriteLine("経過時間の合計 = {0}", sw.Elapsed);
            Debug.WriteLine(sql.Query);
            sql.Parameters.Select(p => p.Key + " " + p.Value).ToList().ForEach(s => Debug.WriteLine(s));
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