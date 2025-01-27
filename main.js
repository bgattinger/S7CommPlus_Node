const edge = require('edge-js');

var Connect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', // path to .dll
    typeName: 'S7CommPlusDllWrapper.DllWrapper',
    methodName: 'Invoke'
});

console.log("Connecting...");

Connect({ command: "createConnectionObject" }, (error, result) => {
    if (error) throw error;
    console.log(result);
});

Connect({ command: "initiateConnection", IPaddress: "192.168.18.25", password: "", timeout: 5000 }, (error, result) => {
    if (error) throw error;
    console.log(result);
});

Connect({ command: "getDataBlockInfoList" }, (error, result) => {
    if (error) throw error;
    console.log(result);
});

Connect({ command: "readVariable", tagSymbol: "\"DB_HardwareDataTypes\".DB_DYN_1002"}, (error, result) => {
    if (error) throw error;
    console.log(result);
});
