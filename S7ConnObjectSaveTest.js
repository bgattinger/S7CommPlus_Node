const edge = require('edge-js');


const createS7Connection = edge.func(function() {
    /* 
    using System;
    using System.Threading.Tasks;
    using System.Reflection;

    public class Startup 
    {

        private static string dllPath = @"C:\Users\brett\SVS\S7CommPlus_Node\S7CommPlusDriver.dll";
        private static Assembly S7CommPlusAssembly = Assembly.LoadFrom(dllPath);
        private static Type S7CommPlusConnection_Class = S7CommPlusAssembly.GetType("S7CommPlusDriver.S7CommPlusConnection");
        private static MethodInfo S7CommPlusConnection_Class_Method_Connect = S7CommPlusConnection_Class.GetMethod("Connect");
        private static object conn = null;


        public async Task<object> Invoke(string input) 
        {

            if (input == "createConnectionObject") {
                System.Console.WriteLine("1");
                conn = Activator.CreateInstance(S7CommPlusConnection_Class);
                return conn;
            
            } else if (input == "initiateConnection") {

                string ipAddress = "192.168.18.25";  
                string password = ""; 
                int timeout = 5000;
                int connectionResult = (int)S7CommPlusConnection_Class_Method_Connect.Invoke(conn, new object[] { ipAddress, password, timeout});
            
                if (connectionResult != 0)
                {
                    return "Error";
                }
                return "successful";
            }
            return null;
        }
    }
    */
});


createS7Connection("createConnectionObject", function(error, result) {
    if (error) throw error;
    console.log("Connection object created:", result);

    // Initiate connection
    createS7Connection("initiateConnection", function(error, result) {
        if (error) throw error;
        console.log("Connection result:", result);
    });
});
