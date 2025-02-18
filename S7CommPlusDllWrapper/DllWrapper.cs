using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private static Dictionary<UInt32, S7CommPlusConnection> plcConns = new Dictionary<uint, S7CommPlusConnection>();
        private static Dictionary<UInt32, Delegate> dataConversionDict = new Dictionary<uint, Delegate>
        {
            { 1, new Func<object,bool>(input => Convert.ToBoolean(input)) },
            { 2, new Func<object,byte>(input => Convert.ToByte(input)) },
            { 3, new Func<object,char>(input => Convert.ToChar(input)) },
            { 4, new Func<object,UInt16>(input => Convert.ToUInt16(input)) },
            { 5, new Func<object,Int16>(input => Convert.ToInt16(input)) },
            { 6, new Func<object,UInt32>(input => Convert.ToUInt32(input)) },
            { 7, new Func<object,Int32>((input) => Convert.ToInt32(input)) },
            { 8, new Func<object,Single>((input) => Convert.ToSingle(input)) },
            { 9, new Func<object,DateTime>((input) => Convert.ToDateTime(input)) },
            { 10, new Func<object,UInt32>(input => Convert.ToUInt32(input)) },
            { 11, new Func<object,Int32>(input => Convert.ToInt32(input)) },
            { 12, new Func<object,UInt16>(input => Convert.ToUInt16(input)) },
            { 14, new Func<object,DateTime>(input => Convert.ToDateTime(input)) },
            { 19, new Func<object,String>(input => Convert.ToString(input)) },
            { 20, new Func<object,byte[]>(input => {
                if (input is String str) {
                    return System.Text.Encoding.UTF8.GetBytes(str);
                } else if (input is byte[] byteArray) {
                    return byteArray;
                } else {
                    throw new InvalidCastException("Input cannot be converted to byte array");
                }
            })},
            { 22, new Func<object,byte[]>(input => {
                if (input is String str) {
                    return System.Text.Encoding.UTF8.GetBytes(str);
                } else if (input is byte[] byteArray) {
                    return byteArray;
                } else {
                    throw new InvalidCastException("Input cannot be converted to byte array");
                }
            })},
            { 48, new Func<object,double>(input => Convert.ToDouble(input)) },
            { 49, new Func<object,UInt64>(input => Convert.ToUInt64(input)) },
            { 50, new Func<object,Int64>(input => Convert.ToInt64(input)) },
            { 51, new Func<object,UInt64>(input => Convert.ToUInt64(input)) },
            { 52, new Func<object,SByte>(input => Convert.ToSByte(input)) },
            { 53, new Func<object,UInt16>(input => Convert.ToUInt16(input)) },
            { 54, new Func<object,UInt32>(input => Convert.ToUInt32(input)) },
            { 55, new Func<object,SByte>(input => Convert.ToSByte(input)) },
            { 61, new Func<object,char>(input => Convert.ToChar(input)) },
            { 62, new Func<object,string>(input => Convert.ToString(input)) },
            { 64, new Func<object,Int64>(input => Convert.ToInt64(input)) },
            { 65, new Func<object,UInt64>(input => Convert.ToUInt64(input)) },
            { 66, new Func<object,UInt64>(input => Convert.ToUInt64(input)) },
            { 67, new Func<object,DateTime>(input => Convert.ToDateTime(input)) },
        };

        private static Dictionary<UInt32, Func<string, ItemAddress, UInt32, PlcTag>> PlcTagConstructors = 
            new Dictionary<uint, Func<string, ItemAddress, uint, PlcTag>>
            {
                { 1, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagBool), name, address, dataType) },
                { 2, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagByte), name, address, dataType) },
                { 3, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagChar), name, address, dataType) },
                { 4, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagWord), name, address, dataType) },
                { 5, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagInt), name, address, dataType) },
                { 6, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagDWord), name, address, dataType) },
                { 7, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagDInt), name, address, dataType) },
                { 8, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagReal), name, address, dataType) },
                { 9, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagDate), name, address, dataType) },
                { 10, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagTimeOfDay), name, address, dataType) },
                { 11, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagTime), name, address, dataType) },
                { 12, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagS5Time), name, address, dataType) },
                { 14, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagDateAndTime), name, address, dataType) },
                { 19, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagString), name, address, dataType) },
                { 20, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagPointer), name, address, dataType) },
                { 22, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagAny), name, address, dataType) },
                { 48, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagLReal), name, address, dataType) },
                { 49, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagULInt), name, address, dataType) },
                { 50, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagLInt), name, address, dataType) },
                { 51, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagLWord), name, address, dataType) },
                { 52, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagUSInt), name, address, dataType) },
                { 53, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagUInt), name, address, dataType) },
                { 54, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagUDInt), name, address, dataType) },
                { 55, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagSInt), name, address, dataType) },
                { 61, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagWChar), name, address, dataType) },
                { 62, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagWString), name, address, dataType) },
                { 64, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagLTime), name, address, dataType) },
                { 65, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagLTOD), name, address, dataType) },
                { 66, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagLDT), name, address, dataType) },
                { 67, (name, address, dataType) => (PlcTag)Activator.CreateInstance(typeof(PlcTagDTL), name, address, dataType) },
            };
        private static PlcTag CreateTag(string name, ItemAddress address, UInt32 dataType) {
            if (PlcTagConstructors.TryGetValue(dataType, out var constructor)) {
                return constructor(name,address,dataType);
            } // else
            throw new ArgumentException($"unknown dataType: {dataType}");
        }

        private static Dictionary<UInt32, PropertyInfo> PlcTagValueProperties = 
            new Dictionary<UInt32, PropertyInfo>
            {
                { 1, typeof(PlcTagBool).GetProperty("Value") },
                { 2, typeof(PlcTagByte).GetProperty("Value") },
                { 3, typeof(PlcTagChar).GetProperty("Value") },
                { 4, typeof(PlcTagWord).GetProperty("Value") },
                { 5, typeof(PlcTagInt).GetProperty("Value") },
                { 6, typeof(PlcTagDWord).GetProperty("Value") },
                { 7, typeof(PlcTagDInt).GetProperty("Value") },
                { 8, typeof(PlcTagReal).GetProperty("Value") },
                { 9, typeof(PlcTagDate).GetProperty("Value") },
                { 10, typeof(PlcTagTimeOfDay).GetProperty("Value") },
                { 11, typeof(PlcTagTime).GetProperty("Value") },
                { 12, typeof(PlcTagS5Time).GetProperty("Value") },
                { 14, typeof(PlcTagDateAndTime).GetProperty("Value") },
                { 19, typeof(PlcTagString).GetProperty("Value") },
                { 20, typeof(PlcTagPointer).GetProperty("Value") },
                { 22, typeof(PlcTagAny).GetProperty("Value") },
                { 48, typeof(PlcTagLReal).GetProperty("Value") },
                { 49, typeof(PlcTagULInt).GetProperty("Value") },
                { 50, typeof(PlcTagLInt).GetProperty("Value") },
                { 51, typeof(PlcTagLWord).GetProperty("Value") },
                { 52, typeof(PlcTagUSInt).GetProperty("Value") },
                { 53, typeof(PlcTagUInt).GetProperty("Value") },
                { 54, typeof(PlcTagUDInt).GetProperty("Value") },
                { 55, typeof(PlcTagSInt).GetProperty("Value") },
                { 61, typeof(PlcTagWChar).GetProperty("Value") },
                { 62, typeof(PlcTagWString).GetProperty("Value") },
                { 64, typeof(PlcTagLTime).GetProperty("Value") },
                { 65, typeof(PlcTagLTOD).GetProperty("Value") },
                { 66, typeof(PlcTagLDT).GetProperty("Value") },
                { 67, typeof(PlcTagDTL).GetProperty("Value") },
            };
            private static object GetValue(PlcTag tag) {
                if (PlcTagValueProperties.TryGetValue(tag.Datatype, out var valProp)) {
                    return valProp.GetValue(tag);
                } // else
                throw new ArgumentException($"Unknown PlcTag type: {tag.GetType()}");
            }
            private static void SetValue(PlcTag tag, object newVal) {
                if (PlcTagValueProperties.TryGetValue(tag.Datatype, out var valProp)) {
                    valProp.SetValue(tag, newVal);
                    return;
                } // else
                throw new ArgumentException($"Unknown PlcTag type: {tag.GetType()}");
            }
















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
            output.sessionID2 = conn.SessionId2;
            if (output.connRes == 0) {

                // connection successful
                /* DEV NOTE: 
                    Connect() calls to previously connected IPs seem to yield the same sessionID2 value
                    we allow the plcConns dictionary to remember the previous connection and its sessionID2 value
                    which is why we check for it here */
                if (plcConns.ContainsKey(output.sessionID2)) {
                    plcConns[output.sessionID2] = conn;
                } else {
                    plcConns.Add(conn.SessionId2, conn);
                }
                
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
            List<(string tagName, ItemAddress tagAddress, UInt32 tagDataType)>
                targetTagReadProfiles = new List<(string tagName, ItemAddress tagAddress, uint tagDataType)>();
            foreach (var trp in input.tagReadProfiles) {
                targetTagReadProfiles.Add(
                    (
                        tagName: (string)trp.Name,
                        tagAddress: new ItemAddress {
                            SymbolCrc = Convert.ToUInt32(trp.Address.SymbolCrc ?? 0),
                            AccessArea = Convert.ToUInt32(trp.Address.AccessArea ?? 0),
                            AccessSubArea = Convert.ToUInt32(trp.Address.AccessSubArea ?? 0),
                            LID = trp.Address.LID != null 
                                ? ((IEnumerable<object>)trp.Address.LID).Select(obj => Convert.ToUInt32(obj)).ToList() 
                                : new List<UInt32>()
                        },
                        tagDataType: Convert.ToUInt32(trp.Datatype ?? 0)
                    )
                );
            }

            // init output object (initialize to unsuccseful read values)
            var output = (
                readRes: (int)-1, 
                readTags: new List<PlcTag>()
            );

            PlcTags tagsToRead = new PlcTags();
            if (!plcConns.ContainsKey(targetConnSessID)) {
                return output;
            } // else 
            
            // load the tag symbols into PlcTag objects to be read
            foreach (var (tagName, tagAddress, tagDataType) in targetTagReadProfiles) {
                // create tag ojbect
                PlcTag tag = CreateTag(tagName, tagAddress, tagDataType);
                
                if (tag != null) {
                    output.readTags.Add(tag);
                    tagsToRead.AddTag(output.readTags[output.readTags.Count()-1]);
                }   
            }

            // read tag values into PLC tag objects
            output.readRes = await Task.Run(() => tagsToRead.ReadTags(plcConns[targetConnSessID]));

            // return tag objects populated with read values
            return output;
        }



        public async Task<object> WriteTags(dynamic input) {

            // parse input
            UInt32 targetConnSessID = Convert.ToUInt32(input.sessionID2 ?? 0);
            List<(string tagName, ItemAddress tagAddress, UInt32 tagDataType, object tagWriteValue)> 
                targetTagWriteProfiles = new List<(string tagName, ItemAddress tagAddress, uint tagDataType, object tagWriteValue)>();
            foreach (var twp in input.tagWriteProfiles ) {
                targetTagWriteProfiles.Add(
                    (
                        tagName: (string)twp.Name,
                        tagAddress: new ItemAddress {
                            SymbolCrc = Convert.ToUInt32(twp.Address.SymbolCrc ?? 0),
                            AccessArea = Convert.ToUInt32(twp.Address.AccessArea ?? 0),
                            AccessSubArea = Convert.ToUInt32(twp.Address.AccessSubArea ?? 0),
                            LID = twp.Address.LID != null 
                                ? ((IEnumerable<object>)twp.Address.LID).Select(obj => Convert.ToUInt32(obj)).ToList() 
                                : new List<UInt32>()
                        },
                        tagDataType: Convert.ToUInt32(twp.Datatype ?? 0),
                        tagWriteValue: dataConversionDict[Convert.ToUInt32(twp.Datatype ?? 0)](twp.writeValue)
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
            foreach (var (tagName, tagAddress, tagDataType, tagWriteValue) in targetTagWriteProfiles) {
                PlcTag tag = CreateTag(tagName, tagAddress, tagDataType);
                if (tag != null) {
                    SetValue(tag, tagWriteValue);
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