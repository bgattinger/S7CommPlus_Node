const edge = require('edge-js');

var Connect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', // path to .dll
    typeName: 'S7CommPlusDriverManagerInterface.Methods',
    methodName: 'Connect'
});

var GetDatablockInfo = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', // path to .dll
    typeName: 'S7CommPlusDriverManagerInterface.Methods',
    methodName: 'GetDatablockInfo'
});

Connect({ ipAddress: "192.168.18.25", password: "", timeout: 5000 }, (error, result) => {
    if (error) throw error;
    var connRes = result;
    console.log("code: " + result);
    if (connRes == 3) {
        console.log("connection request timed out");
        return;
    }
    if (connRes == 0) {
        console.log("connection established!");

        GetDatablockInfo(null, (error, result) => {
            if (error) throw error;
            console.log(result);
        });

        return;
    }
});



