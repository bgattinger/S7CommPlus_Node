const edge = require('edge-js');

const driver = edge.func(function() {
    /* 
    using System;
    using System.Threading.Tasks;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Runtime.InteropServices;

    public class Startup 
    {

        private static string dllPath = @"C:\Users\brett\SVS\S7CommPlus_Node\dlls\S7CommPlusDriver.dll";
        private static Assembly S7CommPlusAssembly = Assembly.LoadFrom(dllPath);

        private static Type S7CommPlusConnection_Type = S7CommPlusAssembly.GetType("S7CommPlusDriver.S7CommPlusConnection");
        private static MethodInfo S7CommPlusConnection_Method_Connect = S7CommPlusConnection_Type.GetMethod("Connect");
        private static MethodInfo S7CommPlusConnection_Method_GetListOfDatablocks = S7CommPlusConnection_Type.GetMethod("GetListOfDatablocks");

        private static Type DatablockInfo_Type = S7CommPlusAssembly.GetType("S7CommPlusDriver.S7CommPlusConnection+DatablockInfo");

        private static Type DatablockInfoList_Type = typeof(List<>).MakeGenericType(DatablockInfo_Type);

        private object conn = null;
        private object dbInfoList = null;


        public async Task<object> Invoke(dynamic input) 
        {
            string command = (string)input.command;
            var parameters = input.parameters;

            if (command == "createConnectionObject") {
                System.Console.WriteLine("Creating S7CommPlusConnection object...");

                conn = Activator.CreateInstance(S7CommPlusConnection_Type);
                return conn;
            


            } else if (command == "initiateConnection") {
                System.Console.WriteLine("Initiating Connection...");

                string ipAddress = (string)parameters.IPaddress; 
                string password = (string)parameters.password; 
                int timeout = (int)parameters.timeout;
                int connectionResult = (int)S7CommPlusConnection_Method_Connect.Invoke(conn, new object[] { ipAddress, password, timeout});
            
                if (connectionResult != 0)
                {
                    return "Error";
                }
                return "successful";



            } else if (command == "getDataBlockInfoList") {
                System.Console.WriteLine("Getting DataBlockInfo List...");

                dbInfoList = Activator.CreateInstance(DatablockInfoList_Type);
                object[] parameters1 = new object[] { dbInfoList };


                int dataBlockListAccessResult = (int)S7CommPlusConnection_Method_GetListOfDatablocks.Invoke(conn, parameters1);
                if (dataBlockListAccessResult != 0)
                {
                    return "Error";
                }

                dbInfoList = parameters1[0];

                return dbInfoList;


            }
            return null;
        }
    }
    */
});

driver(
    {
        command: "createConnectionObject",
        parameters: {}
    },
    function(error, result) {
        if (error) throw error;
        console.log("Connection object created: ", result);

        // Initiate connection
        driver(
            {
                command: "initiateConnection",
                parameters: {IPaddress: "192.168.18.25", password: "", timeout: 5000}
            },
            function(error, result) {
                if (error) throw error;
                console.log("Connection result: ", result);

                // build treeview
                driver (
                    {
                        command: "getDataBlockInfoList",
                        parameters: {}
                    },
                    function(error, result) {
                        if (error) throw error;
                        console.log("Connection result: ", result);


                    }
                )
            }
        );
    }
);