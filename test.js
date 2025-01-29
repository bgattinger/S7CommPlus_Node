
const edge = require('edge-js');



var Connect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Connect'
});

var GetDataBlockInfoList = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockInfoList'
});

var GetPObject_atDBInfoListIndex = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetPObject_atDBInfoListIndex'
});

var GetDataBlockVariables_atDBInfoListIndex = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockVariables_atDBInfoListIndex'
});

var GetDataBlockNamesandVariables = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockNamesandVariables'
});


var GetTag = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetTag'
});

var Disconnect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Disconnect'
});


const connParams = {
    ipAddress: "192.168.18.25",
    password: "", 
    timeout: 5000
}
Connect(connParams, (error, connRes) => {
    console.log("Establishing connection to PLC");
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    
    if (connRes != 0) {
        console.log("Unable to connect to PLC (connection code: ", connRes, ")");
        return;
    }
    console.log("Successful connection to PLC (connection code: ", connRes, ")");
});

console.log("\n====================================\n");

GetDataBlockInfoList(null, (error, result) => {
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");

GetPObject_atDBInfoListIndex({index: 1}, (error, result) => {
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    console.log(result); 
});

console.log("\n====================================\n");

GetDataBlockVariables_atDBInfoListIndex({index: 1}, (error, result) => {
    if (error) {
        console.log("Error: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");

GetDataBlockNamesandVariables(null, (error, result) => {
    if (error) {
        console.log("Error: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");

GetTag({tagSymbol: "DB_CharacterStrings.String_Hello_World"}, (error, result) => {
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");

console.log("Disconnecting from PLC");
Disconnect(null, (disconnectError, disconnectResult) => {
    if (disconnectError) {
        console.error('Error disconnecting from PLC:', disconnectError);
    } else {
        console.log(disconnectResult);
    }
});