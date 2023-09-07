// See https://aka.ms/new-console-template for more information

using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SMTPServer
{
    public class MyGlobalVariables
    {
        public static bool KeepRunning { get; set; }
        public AutoResetEvent connectionWaitHandle = new AutoResetEvent(false);
    }
    
    class Program
    {
        static void Main()
        {
            // Create the Serilog object
            Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .Enrich.WithProperty("Version", "0.0.1")
                //.ReadFrom.AppSettings()
                .MinimumLevel.Verbose()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}, TID:{ThreadId}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("log.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}, TID:{ThreadId}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            Log.Verbose("Starting application");

            // Create the TCP listening object
            int port = 2525;
            System.Net.IPAddress ListenIPAddress = System.Net.IPAddress.Any;
            var MyTCPlistener = new TcpListener(ListenIPAddress, port);
            MyTCPlistener.Start();

            MyGlobalVariables.KeepRunning = true;

            while (MyGlobalVariables.KeepRunning)
            {
                IAsyncResult result = MyTCPlistener.BeginAcceptTcpClient(SmtpThreadWork.AcceptConnection, MyTCPlistener);
                MyGlobalVariables.connectionWaitHandle.WaitOne(); // Wait until a client has begun handling an event
                MyGlobalVariables.connectionWaitHandle.Reset(); // Reset wait handle or the loop goes as fast as it can (after first request)
            }

            Log.CloseAndFlush();
        }
        class ThreadIdEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                        "ThreadId", Thread.CurrentThread.ManagedThreadId));
            }
        }
    }
    public class SmtpThreadWork
    {
        public static void AcceptConnection(IAsyncResult NewConnection)
        {
            Log.Debug("Starting a new thread");
            TcpListener listener = (TcpListener)NewConnection.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(NewConnection);


        }
    }
    public class ThreadWork
    {
        public static void DoWork()
        {
            for (int i = 0; i < 3; i++)
            {
                Log.Information("Working thread..." + i);
                Thread.Sleep(100);
            }
        }
    }
}