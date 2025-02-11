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
                    List<PVartypeListElement> elementVarTypes = pObj.VartypeList.Elements;

                    // recurse over 1d struct/type array
                    var d1arr = (IOffsetInfoType_1Dim)varType.OffsetInfoType;
                    uint d1arrCount = d1arr.GetArrayElementCount();
                    int d1arrLowerBound = d1arr.GetArrayLowerBounds();
                    for (int i = 0; i < d1arrCount; i++) {
                        for (int j = 0; j < elementVarNames.Count; j++) {
                            string elemVarName = elementVarNames[j];
                            PVartypeListElement elemVarType = elementVarTypes[j];
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
                    var arr1D = (IOffsetInfoType_1Dim)varType.OffsetInfoType;
                    uint arr1DCount = arr1D.GetArrayElementCount();
                    int arr1DLowerBound = arr1D.GetArrayLowerBounds();
                    for (int i = 0; i < arr1DCount; i++) {
                        string fullTagSymbol = absTagSymbol + "." + varName + "[" + (i + arr1DLowerBound) + "]";
                        output.Add(
                            conn.getPlcTagBySymbol(fullTagSymbol)
                        );
                    }
                }
            }
            else if (varType.OffsetInfoType.IsMDim()) {
                if (varType.OffsetInfoType.HasRelation()) 
                {
                    // is Md struct/type array

                    // get variable names and types of each element
                    PObject pObj = conn.getTypeInfoByRelId(
                        ((IOffsetInfoType_Relation)varType.OffsetInfoType).GetRelationId()
                    );
                    List<string> elementVarNames = pObj.VarnameList.Names;
                    List<PVartypeListElement> elementVarTypes = pObj.VartypeList.Elements;


                    // recurse over Md struct/type array
                    var arrMD = (IOffsetInfoType_MDim)varType.OffsetInfoType;
                    uint[] arrMDDims = arrMD.GetMdimArrayElementCount();
                    int arrMDRank = arrMDDims.Aggregate(0, (acc, act) => acc += (act > 0) ? 1 : 0);
                    uint arrMDCount = arrMDDims.Any(d => d > 0) ?
                        arrMDDims.Aggregate(1u, (acc, act) => acc * (act > 0 ? (uint)act : 1u)) : 0;
                    int[] arrMDLowerBounds = arrMD.GetMdimArrayLowerBounds();
                    for(uint flatIndex = 0; flatIndex < arrMDCount; flatIndex++) {

                        uint tempIndex = flatIndex;
                        uint[] indices = new uint[arrMDRank];
                        for (int dim = arrMDRank; dim >=0; dim--) {
                            indices[dim] = (tempIndex % arrMDDims[dim]) + (uint)arrMDLowerBounds[dim];
                            tempIndex /= arrMDDims[dim];
                        }

                        for (int j = 0; j < elementVarNames.Count; j++) {
                            string elemVarName = elementVarNames[j];
                            PVartypeListElement elemVarType = elementVarTypes[j];
                            output.AddRange(
                                getPlcTags(
                                    conn,
                                    absTagSymbol + "." + varName + "[" + string.Join(",", indices) + "]",
                                    elemVarName,
                                    elemVarType
                                )
                            );
                        }
                    }
                }
                else 
                {
                    // is Md primitive array

                    // iterate over Md primitive array
                    var arrMD = (IOffsetInfoType_MDim)varType.OffsetInfoType;
                    uint[] arrMDDims = arrMD.GetMdimArrayElementCount();
                    int arrMDRank = arrMDDims.Aggregate(0, (acc, act) => acc += (act > 0) ? 1 : 0);
                    uint arrMDCount = arrMDDims.Any(d => d > 0) ?
                        arrMDDims.Aggregate(1u, (acc, act) => acc * (act > 0 ? (uint)act : 1u)) : 0;
                    int[] arrMDLowerBounds = arrMD.GetMdimArrayLowerBounds();
                    for(uint flatIndex = 0; flatIndex < arrMDCount; flatIndex++) {

                        uint tempIndex = flatIndex;
                        uint[] indices = new uint[arrMDRank];
                        for (int dim = arrMDRank; dim >=0; dim--) {
                            indices[dim] = (tempIndex % arrMDDims[dim]) + (uint)arrMDLowerBounds[dim];
                            tempIndex /= arrMDDims[dim];
                        }
                        string fullTagSymbol = absTagSymbol + "." + varName + "[" + string.Join(",", indices) + "]";
                        output.Add(
                            conn.getPlcTagBySymbol(fullTagSymbol)
                        );

                    }
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
                    PVartypeListElement structVarType = pObj.VartypeList.Elements[i];
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



        public async Task<object> WriteTags(dynamic input) {

            // parse input
            UInt32 targetConnSessID = Convert.ToUInt32(input.sessionID2 ?? 0);
            List<(
                string tagName, 
                ItemAddress tagAddress, 
                UInt32 tagDataType
            )> targetTagProfiles = new List<(
                string tagName, 
                ItemAddress tagAddress, 
                uint tagDataType
            )>();
            foreach (var tp in input.tagProfiles ) {
                targetTagProfiles.Add(
                    (
                        tagName: (string)tp.Name,
                        tagAddress: new ItemAddress {
                            SymbolCrc = Convert.ToUInt32(tp.address.SymbolCrc ?? 0),
                            AccessArea = Convert.ToUInt32(tp.address.AccessArea ?? 0),
                            AccessSubArea = Convert.ToUInt32(tp.address.AccessSubArea ?? 0),
                            LID = tp.address.LID != null 
                                ? ((IEnumerable<object>)tp.address.LID).Select(obj => Convert.ToUInt32(obj)).ToList() 
                                : new List<UInt32>()
                        },
                        tagDataType: Convert.ToUInt32(tp.Datatype ?? 0)
                    )
                );
            }

            // init output object (initialize to unsuccsseful write values)
            var output = (
                writeRes: (int)-1,
                writeTags: new List<PlcTag>()
            );

            if (!plcConns.ContainsKey(targetConnSessID)) {
                return output;
            } // else

            // build PlcTag objects
            PlcTags tagsToWrite = new PlcTags();
            foreach (var ttp in targetTagProfiles) {
                PlcTag tag = PlcTags.TagFactory(ttp.tagName, ttp.tagAddress, ttp.tagDataType);

                if (tag != null) {
                    output.writeTags.Add(tag);
                    tagsToWrite.AddTag(tag);
                }   
            }

            // read tag values into PLC tag objects
            output.writeRes = await Task.Run(() => tagsToWrite.WriteTags(plcConns[targetConnSessID]));

            return output;
        }















        
    }
}



/*
Cesar's example from the Driver (DONT DELETE YET)
private void writeTagBySymbol(string value)
{
    tbValue.Text = "";
    tbSymbolicAddress.Text = "";

    setStatus("loading...");
    PlcTag tag = conn.getPlcTagBySymbol(tbSymbol.Text);
    setStatus("connected");
    if (tag == null) return;

 

    switch (tag.Datatype)
    {                    
        case Softdatatype.S7COMMP_SOFTDATATYPE_BOOL:
            PlcTagBool boolTag = new PlcTagBool(tag.Name, tag.Address, tag.Datatype);
            boolTag.Value = Convert.ToBoolean(value);
            break;
        case Softdatatype.S7COMMP_SOFTDATATYPE_DINT:
            PlcTagDInt dintTag = new PlcTagDInt(tag.Name, tag.Address, tag.Datatype);
            dintTag.Value = Convert.ToInt32(value);
            break;
        default:
            break;


    }
        
    
    PlcTags tags = new PlcTags();

    tags.WriteTags
    tags.AddTag(tag);
    if (tags.ReadTags(conn) != 0) return;
    tbValue.Text = tag.ToString();
}
*/