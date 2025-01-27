using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;
using System.Reflection;
using S7CommPlusDriverWrapper;

namespace S7CommPlusDriverWrapper
{
    public class DriverManager
    {
        private static S7CommPlusConnection conn = null;
        private static List<S7CommPlusConnection.DatablockInfo> dbInfoList = null;

        public static int Connect_(string ipAddress, string password, int timeout) {
            System.Console.WriteLine("Establishing Connection...");
            conn = new S7CommPlusConnection();
            return conn.Connect(ipAddress, password, timeout);
        }

        public static int GetDatablockInfo_() {
            return conn.GetListOfDatablocks(out dbInfoList);
        }
    }


}

namespace S7CommPlusDriverManagerInterface
{
    class Methods
    {
        public async Task<object> Connect(dynamic input) {
            return await Task.Run(() => DriverManager.Connect_(
                input.ipAddress, input.password, input.timeout
            ));
        }

        public async Task<object> GetDatablockInfo(dynamic input) {
            return await Task.Run(() => DriverManager.GetDatablockInfo_());
        }
    }
}