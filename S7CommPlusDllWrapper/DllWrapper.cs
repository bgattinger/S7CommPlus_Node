using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;

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

        public async Task<object> GetDataBlockInfoList(dynamic input) {

            int dataBlockListAccessResult = conn.GetListOfDatablocks(out dbInfoList);

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
                
            if (tag == null) return null;             

            PlcTags tags = new PlcTags();
            tags.AddTag(tag);
            if (tags.ReadTags(conn) != 0) return null;

            return tag;
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
