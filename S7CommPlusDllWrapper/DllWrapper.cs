using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;


//Note: all function return values are boxed in a Task<object> before being returned
// The values are then unboxed on the javascript side

namespace S7CommPlusDriverWrapper
{
    public class DriverManager
    {
        //private static S7CommPlusConnection conn = null;
        //private static List<S7CommPlusConnection.DatablockInfo> dbInfoList = null;

        private static Dictionary<UInt32, S7CommPlusConnection> plcConns = new Dictionary<uint, S7CommPlusConnection>();
        
        public async Task<object> Connect(dynamic input) {
            // parse input object
            string ipAddress = (string)input.ipAddress;
            string password = (string)input.password;
            int timeout = (int)input.timeout;

            // init output object (initialize to unsuccseful connection values)
            var output = (connRes: (int)-1, sessionID2: (UInt32)0);

            S7CommPlusConnection conn = new S7CommPlusConnection();
            output.connRes = await Task.Run(() => conn.Connect(ipAddress, password, timeout));
            if (output.connRes == 0) {
                // connection successful
                plcConns.Add(conn.SessionId2, conn);
                output.sessionID2 = conn.SessionId2;
            } //else
            // connection unsuccessful 
            
            return output;
        }

        public async Task<object> Disconnect(dynamic input)
        {
            // parse input
            UInt32 targetConnSessID = (UInt32)input.sessionID2;

            // init output object (initialize to unsuccseful disconnect values)
            var output = (int)0;

            if (plcConns.ContainsKey(targetConnSessID)) {
                await Task.Run(() => plcConns[targetConnSessID].Disconnect());
                plcConns.Remove(targetConnSessID);
                output = 1;
            } // else 

            return output;
        }

        /*public async Task<object> GetDataBlockInfoList(dynamic input) {
            List<S7CommPlusConnection.DatablockInfo> dbInfoList;
            int dataBlockListAccessResult = await Task.Run(() => conn.GetListOfDatablocks(out dbInfoList));

            if (dataBlockListAccessResult != 0)
            {
                return "Error";
            }                
            return dbInfoList;
        }

        public async Task<object> GetPObject_atDBInfoListIndex(dynamic input) {
            int index = (int)input.index;
            S7CommPlusConnection.DatablockInfo targetDB = DriverManager.dbInfoList[index];
            UInt32 target_relid = targetDB.db_block_ti_relid;

            PObject pObj = conn.getTypeInfoByRelId(target_relid);

            return pObj;
        }
        //^^^^USE THIS GUY TO GET full DB INFO DETAILS

        public async Task<object> GetDataBlockVariables_atDBInfoListIndex(dynamic input) {
            int index = (int)input.index;
            S7CommPlusConnection.DatablockInfo targetDB = DriverManager.dbInfoList[index];
            UInt32 target_relid = targetDB.db_block_ti_relid;

            PObject pObj = conn.getTypeInfoByRelId(target_relid);

            PVarnameList datablockVars = pObj.VarnameList;

            return datablockVars;
        }

        public async Task<object> GetDataBlockNamesandVariables(dynamic input) {
            Dictionary<string, PVarnameList> DatablockNameVars = new Dictionary<string, PVarnameList>();

            int dataBlockListAccessResult = conn.GetListOfDatablocks(out dbInfoList);
            if (dataBlockListAccessResult != 0)
            {
                return "Error";
            }

            foreach (S7CommPlusConnection.DatablockInfo dbinfo in dbInfoList) {
                DatablockNameVars[dbinfo.db_name] = conn.getTypeInfoByRelId(dbinfo.db_block_ti_relid).VarnameList;
            }

            return DatablockNameVars;
        }

        public async Task<object> GetTag(dynamic input) {
            PlcTag tag = conn.getPlcTagBySymbol(input.tagSymbol);

            System.Console.WriteLine("HERE");
            System.Console.WriteLine(tag);
                
            if (tag == null) return null;             

            PlcTags tags = new PlcTags();
            tags.AddTag(tag);
            //if (tags.ReadTags(conn) != 0) return null;

            return tag;
        }*/

        
    }
}
