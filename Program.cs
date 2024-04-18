using Cassandra;
using Cassandra.Data.Linq;
using System.Diagnostics;
using System.Net;

namespace CassandraInsert
{
    internal class Program
    {
        private static int cnt = 0;

        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var cluster = Cluster.Builder()
                .AddContactPoints(
                new IPEndPoint(IPAddress.Parse("172.21.68.172"), 9042),
                new IPEndPoint(IPAddress.Parse("172.21.68.172"), 9043),
                new IPEndPoint(IPAddress.Parse("172.21.68.172"), 9044)
                ).Build();

            var session = cluster.Connect();
            var sw = new Stopwatch();
            sw.Start();
            var keyspaceNames = session
                .Execute("SELECT * FROM system_schema.keyspaces")
                .Select(row => row.GetValue<string>("keyspace_name"));
            Parallel.For(1, 1000000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, deviceId =>
            //for (var deviceId = 1; deviceId <= 100; deviceId++)
            {
                var random = new Random();
                var statement = session.Prepare("INSERT INTO t1.meter_reading(deviceid, date, event_time, accepted, consumptionstarttime, id, instanceid, quality, readinglocked, receivedtimestamp, t1_status, t1_value, t2_status, t2_value, valid) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);");
                for (var day = 0; day < 1; day++)
                {
                    var batch = new BatchStatement();
                    batch.SetConsistencyLevel(ConsistencyLevel.Quorum);

                    for (var reading = 1; reading <= 96; reading++)
                    {
                        var date = new DateTime(2019, 1, 1).AddDays(day).AddMinutes(reading * 15);
                        var id = Guid.NewGuid().ToString();
                        var value = (deviceId + 1) * 1000000 + day * 96 + reading;
                        var st = statement.Bind(
                            deviceId,
                            date.Date.ToShortDateString(),
                            date,
                            true, date,
                            id, 2,
                            random.Next(5),
                            false,
                            DateTime.Now, 0,
                            value,
                            0,
                            value + 1,
                            random.Next() % 2 == 0);
                        //session.Execute(st);

                        batch.Add(st);

                        if (reading % 20 == 0)
                        {
                            session.Execute(batch);
                            Interlocked.Increment(ref cnt);
                            Console.WriteLine("Batch: {0:N0}", cnt);

                            session.Execute(batch);
                            batch = new BatchStatement();
                            batch.SetConsistencyLevel(ConsistencyLevel.Quorum);
                        }
                    }
                }
            }
                );

            sw.Stop();

            Console.WriteLine("Elapsed: {0}", sw.Elapsed);
        }
    }
}