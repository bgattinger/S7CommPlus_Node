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

        public async Task<object> Connect(dynamic input) {
            string ipAddress = (string)input.ipAddress;
            string password = (string)input.password;
            int timeout = (int)input.timeout;

            conn = new S7CommPlusConnection();

            int connRes = conn.Connect(ipAddress, password, timeout);
            return connRes;
        }

        public async Task<object> Disconnect(dynamic input)
        {
            if (conn != null)
            {
                conn.Disconnect();
                conn = null;
                return "Disconnected Successfully";
            }
            return "No Active Connection";
        }
    }
}
