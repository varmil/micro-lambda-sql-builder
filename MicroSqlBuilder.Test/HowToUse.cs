using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroSqlBuilder.Test
{
    [TestClass]
    public class HowToUse
    {
        [TestMethod]
        public void And()
        {
            var weapon = "Masamune";
            var sql = new Sqlam<Employee>(e => e.Id < 10)
                .And(e => e.Weapon1 == weapon)
            ;
            var expected = TrimSql("WHERE `id` < @__P1 AND `weapon1` = @__P2");
            var actual = TrimSql(sql.Query);

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(10, sql.Parameters["__P1"]);
            Assert.AreEqual(weapon, sql.Parameters["__P2"]);
        }

        [TestMethod]
        public void WhereIn()
        {
            var list = new List<string>() { "A", "BB", "CCC" };
            var sql = new Sqlam<Employee>()
                .In(e => e.FirstName, list)
            ;
            var expected = TrimSql("WHERE `first_name` IN (@__P1,@__P2,@__P3)");
            var actual = TrimSql(sql.Query);

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(list[2], sql.Parameters["__P3"]);
        }

        [TestMethod]
        public void OrderBy()
        {
            var sql = new Sqlam<Employee>(e => e.CreatedAt > DateTime.MinValue)
                .OrderBy(e => e.Id, Order.Desc)
                .OrderBy(e => e.FirstName)
            ;
            var expected = TrimSql("WHERE `created_at` > @__P1 ORDER BY `id` DESC, `first_name`");
            var actual = TrimSql(sql.Query);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LimitOffset()
        {
            var number = 10;
            var sql = new Sqlam<Employee>(e => e.Id > 0)
                .Limit(number)
                .Offset(number)
            ;
            var expected = TrimSql("WHERE `id` > @__P1 LIMIT 10 OFFSET 10");
            var actual = TrimSql(sql.Query);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>文字列中の2つ以上のスペースを1つに変換し、行頭行末のスペースを消す</summary>
        private string TrimSql(string str)
        {
            return Regex.Replace(str, @"\s+", " ").Trim();
        }
    }
}


