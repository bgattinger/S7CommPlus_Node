using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Threading.Tasks;
using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;
//using System.Windows.Forms;

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
            var output = (
                connRes: (int)-1, 
                sessionID2: (UInt32)0
            );

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

        public async Task<object> GetDataBlockInfoList(dynamic input) {

            // parse input
            UInt32 targetConnSessID = (UInt32)input.sessionID2;

            // init output object (initialize to unsuccseful read values)
            var output = (
                accessRes: (int)-1, 
                dbInfoList: new List<S7CommPlusConnection.DatablockInfo>()
            );

            if (plcConns.ContainsKey(targetConnSessID)) {
                output.accessRes = await Task.Run(
                    () => plcConns[targetConnSessID].GetListOfDatablocks(out output.dbInfoList)
                );
            } // else

            return output;
        }

        public async Task<object> ReadTags(dynamic input) {

            // parse input
            UInt32 targetConnSessID = (UInt32)input.sessionID2;
            string[] tagSymbols = ((IEnumerable<object>)input.tagSymbols).Cast<string>().ToArray();

            // init output object (initialize to unsuccseful read values)
            var output = (
                accessRes: (int)-1, 
                readTags: new List<PlcTag>()
            );

            PlcTags tagsToRead = new PlcTags();
            if (plcConns.ContainsKey(targetConnSessID)) {

                // load the tag symbols into PlcTag objects to be read
                foreach (string tagSymbol in tagSymbols) {
                    // create tag ojbect
                    PlcTag tag = plcConns[targetConnSessID].getPlcTagBySymbol(tagSymbol);
                    
                    if (tag != null) {
                        output.readTags.Add(tag);
                        tagsToRead.AddTag(output.readTags[output.readTags.Count()-1]);
                    }   
                }

                // read tag values into PLC tag objects
                output.accessRes = await Task.Run(() => tagsToRead.ReadTags(plcConns[targetConnSessID]));
            }

            // return tag objects populated with read values
            return output;
        }


















        private List<PlcTag> getPlcTags(
            S7CommPlusConnection conn,
            string absTagSymbol,
            string varName,
            S7CommPlusDriver.PVartypeListElement varType
        ) {
            List<PlcTag> output = new List<PlcTag>();

            if (varType.OffsetInfoType.Is1Dim()) 
            {
                if (varType.OffsetInfoType.HasRelation()) 
                {
                    // is 1d struct/type array

                    // get variable names and types of each element
                    PObject pObj = conn.getTypeInfoByRelId(
                        ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId()
                    );
                    List<string> elementVarNames = pObj.VarnameList.Names;
                    List<S7CommPlusDriver.PVartypeListElement> elementVarTypes = pObj.VartypeList.Elements;

                    // recurse over 1d struct/type array
                    var d1arr = (IOffsetInfoType_1Dim)varType.OffsetInfoType;
                    uint d1arrCount = d1arr.GetArrayElementCount();
                    int d1arrLowerBound = d1arr.GetArrayLowerBounds();
                    for (int i = 0; i < d1arrCount; i++) {
                        for (int j = 0; j < elementVarNames.Count; j++) {
                            string elemVarName = elementVarNames[j];
                            S7CommPlusDriver.PVartypeListElement elemVarType = elementVarTypes[j];
                            output.AddRange(
                                getPlcTags(
                                    conn,
                                    absTagSymbol + "." + varName + "[" + (i + d1arrLowerBound) + "]",
                                    elemVarName,
                                    elemVarType
                                )
                            );
                        }

                    }
                }
                else 
                {
                    // is 1d primtive array

                    // iterate over 1d primitive array
                    var d1arr = (IOffsetInfoType_1Dim)varType.OffsetInfoType;
                    uint d1arrCount = d1arr.GetArrayElementCount();
                    int d1arrLowerBound = d1arr.GetArrayLowerBounds();
                    for (int i = 0; i < d1arrCount; i++) {
                        string fullTagSymbol = absTagSymbol + "." + varName + "[" + (i + d1arrLowerBound) + "]";
                        output.Add(
                            conn.getPlcTagBySymbol(fullTagSymbol)
                        );
                    }
                }
            }
            else if (varType.OffsetInfoType.IsMDim()) {
                if (varType.OffsetInfoType.HasRelation()) 
                {
                    // TODO
                }
                else 
                {
                    // TODO
                }
            }
            else if (varType.OffsetInfoType.HasRelation())
            {
                // is struct/type
                
                // recurse on each element of struct/type
                PObject pObj = conn.getTypeInfoByRelId(
                    ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId()
                );
                for (int i = 0; i < pObj.VarnameList.Names.Count; i++){
                    string structVarName = pObj.VarnameList.Names[i];
                    S7CommPlusDriver.PVartypeListElement structVarType = pObj.VartypeList.Elements[i];
                    output.AddRange(
                        getPlcTags(
                            conn,
                            absTagSymbol + "." + varName,
                            structVarName,
                            structVarType
                        )
                    );
                }
            }
            else 
            {
                // is single tag/value
                string fullTagSymbol = absTagSymbol + "." + varName;
                output.Add(
                    conn.getPlcTagBySymbol(fullTagSymbol)
                );
            }

            return output;
        }
        public async Task<object> GetDataBlockPlcTags(dynamic input) {

            // parse input
            UInt32 targetConnSessID = (UInt32)input.sessionID2;
            string targetDataBlockName = (string)input.dataBlockName;

            // init output object 
            var output = (
                plcTagAccRes: (int)0,
                plcTagAccErr: (string)"",
                plcTagList: new List<PlcTag>()
            );

            // get plc connection if it exists
            if (!plcConns.ContainsKey(targetConnSessID)) {
                output.plcTagAccRes = 0;
                output.plcTagAccErr = "Error: No such PLC connection exists";
                return output;
            }
            S7CommPlusConnection conn = plcConns[targetConnSessID];

            // search for target datablock by name
            List<S7CommPlusConnection.DatablockInfo> dbInfoList = null;
            int accessRes = plcConns[targetConnSessID].GetListOfDatablocks(out dbInfoList);
            if (accessRes != 0) {
                output.plcTagAccRes = 0;
                output.plcTagAccErr = "Error: Unable to Access Datablock Information List";
                return output;
            }
            S7CommPlusConnection.DatablockInfo targetDataBlock = new S7CommPlusConnection.DatablockInfo(); 
            foreach ( S7CommPlusConnection.DatablockInfo dataBlock in dbInfoList) {
                if (dataBlock.db_name == targetDataBlockName) {
                    targetDataBlock = dataBlock;
                    break;
                }
            }
            if (targetDataBlock is null) {
                output.plcTagAccRes = 0;
                output.plcTagAccErr = "Error: Datablock: " + targetDataBlockName + " not found";
                return output;
            }

            // get target Datablock's PObject
            PObject targetDBpObj = conn.getTypeInfoByRelId(targetDataBlock.db_block_ti_relid);

            try {
                for (int i = 0; i < targetDBpObj.VarnameList.Names.Count; ++i) {
                    string varName = targetDBpObj.VarnameList.Names[i];
                    S7CommPlusDriver.PVartypeListElement varType = targetDBpObj.VartypeList.Elements[i];
                    output.plcTagList.AddRange(
                        getPlcTags(
                            conn,
                            targetDataBlockName,
                            varName,
                            varType
                        )
                    );
                }

            } catch (Exception ex) {
                output.plcTagAccRes = 0;
                output.plcTagAccErr = ex.Message;
                return output;
            }
            
            output.plcTagAccRes = 1;
            output.plcTagAccErr = "Successfully retrieved " + targetDataBlockName + "tags";
            return output;
        }

        


















        public List<object> recurse_probeDB(
            UInt32 targetConnSessID,
            string varName, 
            S7CommPlusDriver.PVartypeListElement varType 
        ){
                List<object> output = new List<object>();
                S7CommPlusConnection conn = plcConns[targetConnSessID];

                output.Add(" <var = " + varName + "> ");

                if (varType.OffsetInfoType.Is1Dim()) 
                {
                    // is 1d array
                    if (varType.OffsetInfoType.HasRelation()) {
                        output.Add(" <datatype = 1d_Struct/Type_Array> ");
                        
                        // get variable names and types of each element
                        PObject pObj = plcConns[targetConnSessID].getTypeInfoByRelId(
                            ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId()
                        );
                        List<string> elementVarNames = pObj.VarnameList.Names;
                        List<S7CommPlusDriver.PVartypeListElement> elementVarTypes = pObj.VartypeList.Elements;

                        // recurse over 1d array
                        List<object> arrayContent = new List<object>(); 
                        var d1arr = (IOffsetInfoType_1Dim)varType.OffsetInfoType;
                        uint d1arrCount = d1arr.GetArrayElementCount();
                        int d1arrLowerBound = d1arr.GetArrayLowerBounds();
                        for (int i = 0; i < d1arrCount; i++) {
                            
                            var element = (
                                elementName: varName + "[" + i + "]",
                                elementContent: new List<object>()
                            );

                            for (int j = 0; j < elementVarNames.Count; j++) {
                                string elemVarName = elementVarNames[j];
                                S7CommPlusDriver.PVartypeListElement elemVarType = elementVarTypes[j];
                                element.elementContent.Add(
                                    recurse_probeDB(
                                        targetConnSessID,
                                        elemVarName,
                                        elemVarType
                                    )   
                                );
                            }

                            arrayContent.Add(element);
                        }
                        output.Add(arrayContent);

                    } else {
                        output.Add(" <datatype = 1d_Primitive_Array> ");

                        // iterate over 1d array
                        List<string> arrayContent = new List<string>();
                        var d1arr = (IOffsetInfoType_1Dim)varType.OffsetInfoType;
                        uint d1arrCount = d1arr.GetArrayElementCount();
                        int d1arrLowerBound = d1arr.GetArrayLowerBounds();
                        for (int i = 0; i < d1arrCount; i++) {
                            arrayContent.Add(varName + "[" + i + "]");

                        }
                        output.Add(arrayContent);
                    }

                }
                else if (varType.OffsetInfoType.IsMDim()) 
                {
                    // is Md array
                    if (varType.OffsetInfoType.HasRelation()) {
                        output.Add(varName + " <datatype = Md_Struct/Type_Array> ");
                        //TODO

                    } else {
                        output.Add(varName + " <datatype = Md_Primitive_Array> ");
                        //TODO
                        
                    }
                }
                else if (varType.OffsetInfoType.HasRelation()) 
                {
                    // is struct/type
                    output.Add(" <datatype = struct/type> ");

                    // recurse on each element of struct
                    PObject pObj = plcConns[targetConnSessID].getTypeInfoByRelId(
                        ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId()
                    );
                    List<string> structVarNames = pObj.VarnameList.Names;
                    List<S7CommPlusDriver.PVartypeListElement> structVarTypes = pObj.VartypeList.Elements;

                    List<object> structContent = new List<object>(); 
                    for (int i = 0; i < structVarNames.Count; i++) {
                        string elemVarName = structVarNames[i];
                        S7CommPlusDriver.PVartypeListElement elemVarType = structVarTypes[i];
                        structContent.Add(
                            recurse_probeDB(
                                targetConnSessID,
                                elemVarName,
                                elemVarType
                            )
                        );
                    }
                    output.Add(structContent);
                } 
                else
                {
                    // is single valued
                    output.Add(" <datatype = primitive> ");
                }

                return output;
        }
        public async Task<object> ProbeDataBlock(dynamic input) {

            // parse input
            UInt32 targetConnSessID = (UInt32)input.sessionID2;
            string targetDataBlockName = (string)input.dataBlockName;

            // init output object 
            var output = new List<object>();

            // check if connection exists
            if (!plcConns.ContainsKey(targetConnSessID)) {
                output.Add("Error no such connection");
                return output;
            }

            // get target datablock 
            List<S7CommPlusConnection.DatablockInfo> dbInfoList = null;
            int accessRes = plcConns[targetConnSessID].GetListOfDatablocks(out dbInfoList);
            if (accessRes != 0) {
                output.Add("Error executing GetListofDataBlocks()");
                return output;
            }
            S7CommPlusConnection.DatablockInfo targetDataBlock = null; 
            foreach ( S7CommPlusConnection.DatablockInfo dataBlock in dbInfoList) {
                if (dataBlock.db_name == targetDataBlockName) {
                    targetDataBlock = dataBlock;
                    break;
                }
            }
            if (targetDataBlock is null) {
                output.Add("Error: DataBlock with name: " + targetDataBlockName + " not found");
                return output;
            }

            output.Add("Target DataBlock ID: " + targetDataBlock.db_block_ti_relid);
            output.Add("===================");

            // get target datablock pObject
            PObject targetDBpObj = plcConns[targetConnSessID].getTypeInfoByRelId(targetDataBlock.db_block_ti_relid);

            // begin probing target data block
            Dictionary<string, string> trgtDBVarInfo = new Dictionary<string, string>();
            for (int i = 0; i < targetDBpObj.VarnameList.Names.Count; ++i) {

                string varName = targetDBpObj.VarnameList.Names[i];
                S7CommPlusDriver.PVartypeListElement varType = targetDBpObj.VartypeList.Elements[i];
                output.Add(recurse_probeDB(targetConnSessID, varName, varType));
                
            }
            output.Add(trgtDBVarInfo);
            output.Add("===================");

            return output;
        }


















        
    }
}



































































/*

public async Task<object> GetPlcTagSymbols(dynamic input) {

            // parse input
            UInt32 targetConnSessID = (UInt32)input.sessionID2;

            // init output object
            var output = (
                dbInfoListAccesRes: (int)-1,
                tagSymbols: new List<string>()
            ); 

            // check if PLC connection exists
            if (!plcConns.ContainsKey(targetConnSessID)) {
                return output;
            }

            // access PLC datablock information list
            List<S7CommPlusConnection.DatablockInfo> dbInfoList = null;
            output.dbInfoListAccesRes = plcConns[targetConnSessID].GetListOfDatablocks(out dbInfoList);
            if (output.dbInfoListAccesRes != 0 ) {
                return output;
            }

            async Task<List<string>> GetTagSymbols(uint parentRELID, string parentSymbol) {
                List<string> result = new List<string>();

                PObject pObj = plcConns[targetConnSessID].getTypeInfoByRelId(parentRELID);
                for (int i = 0; i < pObj.VarnameList.Names.Count; ++i) {

                    if (pObj.VartypeList.Elements[i].OffsetInfoType.Is1Dim()) 
                    {
                        

                        if (pObj.VartypeList.Elements[i].OffsetInfoType.HasRelation()) 
                        {
                            // recurse over 1d array of structs (do asynchronously)
                            List<Task> tasks = new List<Task>();
                            
                        } 
                        else 
                        {
                            // get 1d array of tag symbols
                            var d1arr = (IOffsetInfoType_1Dim)pObj.VartypeList.Elements[i].OffsetInfoType;
                            uint d1arrCount = d1arr.GetArrayElementCount();
                            int d1arrLowerBound = d1arr.GetArrayLowerBounds();
                            for (int j = 0 ; j < d1arrCount; ++j) {
                                result.Add(parentSymbol + "." + pObj.VarnameList.Names[i] + "[" + (j + d1arrLowerBound) + "]" );
                            }
                        }
                    } 
                    else if (pObj.VartypeList.Elements[i].OffsetInfoType.IsMDim()) 
                    {
                        if (pObj.VartypeList.Elements[i].OffsetInfoType.HasRelation()) 
                        {
                            // recurse over md array of structs (do asynchronously)
                        } 
                        else 
                        {
                            // get md array of tag symbols
                            var dMarr = (IOffsetInfoType_MDim)pObj.VartypeList.Elements[i].OffsetInfoType;
                            uint[] dMarrDims = dMarr.GetMdimArrayElementCount();
                            int dMarrRank = dMarrDims.Aggregate(0, (acc, act) => acc += (act > 0) ? 1 : 0);
                            uint dMarrCount = dMarrDims.Aggregate(1u, (acc, act) => acc * (act > 0 ? (uint)act : 1u));
                            int[] dMarrLowerBounds = dMarr.GetMdimArrayLowerBounds();
                            uint[] indices = new uint[dMarrRank];
                            for(uint flatIndex = 0; flatIndex < dMarrCount; flatIndex++) {
                                for (int dim = dMarrRank; dim >=0; dim--) {
                                    indices[dim] = flatIndex % dMarrDims[dim];
                                    flatIndex /= dMarrCount;
                                }
                                result.Add(parentSymbol + "." + pObj.VarnameList.Names[i] + "[" + string.Join(",", indices) + "]");
                            }
                        }
                    }
                    else {
                        if (pObj.VartypeList.Elements[i].OffsetInfoType.HasRelation()) 
                        {
                            // recurse on struct (can do this asynchronously?)
                            result.AddRange( await Task.Run(() => 
                                GetTagSymbols(pObj.RelationId,pObj.VarnameList.Names[i])
                            ));
                        }
                        else 
                        {
                            // get single tag symbol
                            result.Add(parentSymbol + "." + pObj.VarnameList.Names[i]);
                        }
                    }
                }

                return result;
            }   

            foreach (S7CommPlusConnection.DatablockInfo dbInfo in dbInfoList) {
                //Start Recursion here
            }

            return output;
        }



        public async Task<object> ProbeDataBlock(dynamic input) {

            // parse input
            UInt32 targetConnSessID = (UInt32)input.sessionID2;
            string targetDataBlockName = (string)input.dataBlockName;

            // init output object 
            var output = new List<object>();

            // check if connection exists
            if (!plcConns.ContainsKey(targetConnSessID)) {
                output.Add("Error no such connection");
                return output;
            }

            // get target datablock 
            List<S7CommPlusConnection.DatablockInfo> dbInfoList = null;
            int accessRes = plcConns[targetConnSessID].GetListOfDatablocks(out dbInfoList);
            if (accessRes != 0) {
                output.Add("Error executing GetListofDataBlocks()");
                return output;
            }
            S7CommPlusConnection.DatablockInfo targetDataBlock = null; 
            foreach ( S7CommPlusConnection.DatablockInfo dataBlock in dbInfoList) {
                if (dataBlock.db_name == targetDataBlockName) {
                    targetDataBlock = dataBlock;
                    break;
                }
            }
            if (targetDataBlock is null) {
                output.Add("Error: DataBlock with name: " + targetDataBlockName + " not found");
                return output;
            }

            output.Add("Target DataBlock ID: " + targetDataBlock.db_block_ti_relid);
            output.Add("===================");

            // get target datablock pObject
            PObject targetDBpObj = plcConns[targetConnSessID].getTypeInfoByRelId(targetDataBlock.db_block_ti_relid);

            // begin probing target data block
            Dictionary<string, string> trgtDBVarInfo = new Dictionary<string, string>();
            for (int i = 0; i < targetDBpObj.VarnameList.Names.Count; ++i) {
                string varName = targetDBpObj.VarnameList.Names[i];
                S7CommPlusDriver.PVartypeListElement varType = targetDBpObj.VartypeList.Elements[i];

                if (varType.OffsetInfoType.Is1Dim()) {
                    trgtDBVarInfo[varName] = "[ is 1D array ]";

                    if (varType.OffsetInfoType.HasRelation()) {
                        trgtDBVarInfo[varName] += "[ has relid: " + 
                            ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId() + " ]";

                        trgtDBVarInfo[varName] += "[ elements hold: ";  
                        PObject pObj = plcConns[targetConnSessID].getTypeInfoByRelId(
                            ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId()
                        );
                        for (int j = 0; j < pObj.VarnameList.Names.Count; j++) {
                            string innerVarName =  pObj.VarnameList.Names[j];
                            S7CommPlusDriver.PVartypeListElement innerVarType = pObj.VartypeList.Elements[j];

                            trgtDBVarInfo[varName] += " / " + innerVarName;

                            if (pObj.VartypeList.Elements[j].OffsetInfoType.HasRelation()) {
                                trgtDBVarInfo[varName] += 
                                    "[ has relid: " +
                                    ((IOffsetInfoType_Relation)innerVarType.OffsetInfoType).GetRelationId() + " ]";

                                trgtDBVarInfo[varName] += "[ holds: "; 
                                PObject pObj1 = plcConns[targetConnSessID].getTypeInfoByRelId(
                                    ((IOffsetInfoType_Relation)pObj.VartypeList.Elements[j].OffsetInfoType).GetRelationId()
                                );
                                for (int k = 0; k < pObj1.VarnameList.Names.Count; k++) {
                                    string ininVarName =  pObj1.VarnameList.Names[k];
                                    S7CommPlusDriver.PVartypeListElement ininVarType = pObj1.VartypeList.Elements[k];

                                    trgtDBVarInfo[varName] += " // " + ininVarName;
                                }
                                trgtDBVarInfo[varName] += " ]"; 
                            }
                        }
                        trgtDBVarInfo[varName] += " ]"; 


                    }

                } else if (targetDBpObj.VartypeList.Elements[i].OffsetInfoType.IsMDim()) {
                    trgtDBVarInfo[varName] = "is M Dimensional";

                } else {
                    trgtDBVarInfo[varName] = "[ is single value ]";

                    if (varType.OffsetInfoType.HasRelation()) {
                        trgtDBVarInfo[varName] += 
                            "[ has relid: " + 
                            ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId() + " ]";
                        
                        trgtDBVarInfo[varName] += "[ holds: "; 
                        PObject pObj = plcConns[targetConnSessID].getTypeInfoByRelId(
                            ((IOffsetInfoType_Relation)targetDBpObj.VartypeList.Elements[i].OffsetInfoType).GetRelationId()
                        );
                        for (int j = 0; j < pObj.VarnameList.Names.Count; j++) {
                            string innerVarName =  pObj.VarnameList.Names[j];
                            S7CommPlusDriver.PVartypeListElement innerVarType = pObj.VartypeList.Elements[j];

                            trgtDBVarInfo[varName] += " / " + innerVarName;

                            if (pObj.VartypeList.Elements[j].OffsetInfoType.HasRelation()) {
                                trgtDBVarInfo[varName] += 
                                    "[ has relid: " +
                                    ((IOffsetInfoType_Relation)innerVarType.OffsetInfoType).GetRelationId() + " ]";

                                trgtDBVarInfo[varName] += "[ holds: "; 
                                PObject pObj1 = plcConns[targetConnSessID].getTypeInfoByRelId(
                                    ((IOffsetInfoType_Relation)pObj.VartypeList.Elements[j].OffsetInfoType).GetRelationId()
                                );
                                for (int k = 0; k < pObj1.VarnameList.Names.Count; k++) {
                                    string ininVarName =  pObj1.VarnameList.Names[k];
                                    S7CommPlusDriver.PVartypeListElement ininVarType = pObj1.VartypeList.Elements[k];

                                    trgtDBVarInfo[varName] += " // " + ininVarName;
                                }
                                trgtDBVarInfo[varName] += " ]"; 
                            }
                        }
                        trgtDBVarInfo[varName] += " ]"; 
                    }
                }
            }
            output.Add(trgtDBVarInfo);
            output.Add("===================");

            return output;
        }
*/