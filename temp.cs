using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;
using System.Reflection;

namespace S7CommPlusDllWrapper
{
    public class DllWrapper
    {

        private static string dllPath = @".\S7CommPlusDriver\bin\x64\Debug\S7CommPlusDriver.dll"; //path is relative to working directory at runtime (i.e this path value depends on where you run main.js)
        private static Assembly S7CommPlusAssembly = Assembly.LoadFrom(dllPath);

        private static Type S7CommPlusConnection_Type = S7CommPlusAssembly.GetType("S7CommPlusDriver.S7CommPlusConnection");
        private static MethodInfo S7CommPlusConnection_Method_Connect = S7CommPlusConnection_Type.GetMethod("Connect");
        private static MethodInfo S7CommPlusConnection_Method_GetListOfDatablocks = S7CommPlusConnection_Type.GetMethod("GetListOfDatablocks");
        private static MethodInfo S7CommPlusConnection_Method_GetTagBySymbol = S7CommPlusConnection_Type.GetMethod("getPlcTagBySymbol");

        private static Type DatablockInfo_Type = S7CommPlusAssembly.GetType("S7CommPlusDriver.S7CommPlusConnection+DatablockInfo");

        private static Type PlcTag_Type = S7CommPlusAssembly.GetType("S7CommPlusDriver.ClientAPI+PLCTag");

        private static Type DatablockInfoList_Type = typeof(List<>).MakeGenericType(DatablockInfo_Type);

        private object conn = null;
        private object dbInfoList = null;
        private object tag = null;


        public async Task<object> Invoke(dynamic input)
        {
            string command = (string)input.command;

            if (command == "createConnectionObject")
            {
                System.Console.WriteLine("Creating S7CommPlusConnection object...");

                conn = Activator.CreateInstance(S7CommPlusConnection_Type);
                return conn;



            }
            else if (command == "initiateConnection")
            {
                System.Console.WriteLine("Initiating Connection...");

                string ipAddress = (string)input.IPaddress;
                string password = (string)input.password;
                int timeout = (int)input.timeout;
                int connectionResult = (int)S7CommPlusConnection_Method_Connect.Invoke(conn, new object[] { ipAddress, password, timeout });

                if (connectionResult != 0)
                {
                    return "Error";
                }
                return "successful";



            }
            else if (command == "getDataBlockInfoList")
            {
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
            else if (command == "readVariable")
            {

                System.Console.WriteLine("loading...");

                tag = S7CommPlusConnection_Method_GetTagBySymbol.Invoke(conn, new object[] { input.tagSymbol });

           

               //System.Console.WriteLine(tag.ToString());

                return tag.ToString();

            }

            return null;
        }
    }
}