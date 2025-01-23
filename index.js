const edge = require('edge-js');

const pingPLC_test = edge.func(function() {
    /*
    using System;
    using System.Threading.Tasks;
    using System.Reflection;

    public class Startup 
    {
        public async Task<object> Invoke(object input) 
        {

            string dllPath = @"C:\Users\brett\SVS\S7CommPlus_Node\S7CommPlusDriver.dll";
            var s7CommPlusAssembly = Assembly.LoadFrom(dllPath);

            System.Console.WriteLine("1");

            var s7CommPlusConnection_Class = s7CommPlusAssembly.GetType("S7CommPlusDriver.S7CommPlusConnection");
            var conn = Activator.CreateInstance(s7CommPlusConnection_Class);

            System.Console.WriteLine("2");

            var s7CommPlusConnection_Class_Method_Connect = s7CommPlusConnection_Class.GetMethod("Connect");

            System.Console.WriteLine("3");

            string ipAddress = "192.168.18.25";  
            string password = ""; 
            int timeout = 5000;

            int connectionResult = (int)s7CommPlusConnection_Class_Method_Connect.Invoke(conn, new object[] { ipAddress, password, timeout});

            System.Console.WriteLine(connectionResult);

            if (connectionResult != 0)
            {
                return "Error connecting";
            }
            return "Connection successful";
        }
    }
    */
});

pingPLC_test(null, (error, result) => {
    if (error) throw error;
    console.log(result);
});